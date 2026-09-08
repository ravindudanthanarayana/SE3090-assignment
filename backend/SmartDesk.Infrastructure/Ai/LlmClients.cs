using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;

namespace SmartDesk.Infrastructure.Ai;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"gemini" for the live provider, "scripted" for the deterministic offline client.</summary>
    public string Provider { get; set; } = "scripted";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.1-flash-lite";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}

/// <summary>
/// Google Gemini client. Chosen because its free tier lets the whole Agentic AI subsystem run at no
/// cost, which spec section 14 requires ("no paid subscriptions"). The API key comes from the
/// AI_API_KEY environment variable and is never logged, persisted or returned by any endpoint.
/// </summary>
public sealed class GeminiLlmClient(HttpClient http, LlmOptions options, ILogger<GeminiLlmClient> logger) : ILlmClient
{
    public string ProviderName => $"gemini:{options.Model}";

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new DownstreamUnavailableException("No AI API key is configured.");

        var url = $"{options.BaseUrl}/models/{options.Model}:generateContent?key={options.ApiKey}";

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userContent } } } },
            generationConfig = new
            {
                // JSON response mode: the model is constrained to emit a JSON object, which is the
                // first half of our structured-output guarantee. AgentOutputValidator is the second.
                response_mime_type = "application/json",
                temperature = 0.2,
                maxOutputTokens = 2048
            }
        };

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(url, payload, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new DownstreamUnavailableException("The AI provider timed out.");
        }
        catch (HttpRequestException ex)
        {
            throw new DownstreamUnavailableException($"The AI provider is unreachable: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            // Log the status but never the URL, because the URL carries the API key.
            logger.LogWarning("Gemini returned {Status}", response.StatusCode);
            throw new DownstreamUnavailableException(
                $"The AI provider returned {(int)response.StatusCode}. {Truncate(body)}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return ExtractText(json);
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new DownstreamUnavailableException("The AI provider returned no candidates.");

        var candidate = candidates[0];

        // A blocked or truncated response has a finishReason other than STOP and may carry no text
        // at all, so report that specifically rather than failing with a confusing parse error.
        if (candidate.TryGetProperty("finishReason", out var finish) &&
            finish.GetString() is { } reason &&
            !reason.Equals("STOP", StringComparison.OrdinalIgnoreCase))
        {
            throw new DownstreamUnavailableException($"The AI provider stopped early: {reason}.");
        }

        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            throw new DownstreamUnavailableException("The AI provider returned an empty response.");
        }

        // Newer Gemini models may emit reasoning parts before the answer, so take the first part
        // that actually carries text rather than assuming it is parts[0]. We read only the text -
        // any reasoning metadata is deliberately ignored and never persisted.
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text) &&
                text.GetString() is { Length: > 0 } value)
            {
                return value;
            }
        }

        throw new DownstreamUnavailableException("The AI provider returned no text content.");
    }

    private static string Truncate(string s) => s.Length <= 300 ? s : s[..300];
}

/// <summary>
/// Deterministic offline client. Every agent evaluation test runs against this, so the golden cases
/// assert real behaviour without network access, cost or model variance - which is exactly what
/// spec section 12 asks for when it says LLM-as-a-judge must not be the only evaluation method.
///
/// It is also the fallback that lets the whole system be demonstrated with no API key at all.
/// </summary>
public sealed class ScriptedLlmClient : ILlmClient
{
    private readonly Func<string, string, string> _responder;

    public string ProviderName => "scripted";

    public ScriptedLlmClient() : this(DefaultResponder) { }

    /// <summary>Tests inject their own responder to script failures, malformed JSON or specific outputs.</summary>
    public ScriptedLlmClient(Func<string, string, string> responder) => _responder = responder;

    public Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_responder(systemPrompt, userContent));
    }

    /// <summary>
    /// Recognises which agent is calling from a distinctive phrase in its system prompt and returns a
    /// schema-valid response for that agent. It reads the supplied data (categories, candidate ids)
    /// so the responses stay consistent with the real database.
    /// </summary>
    private static string DefaultResponder(string systemPrompt, string userContent)
    {
        if (systemPrompt.Contains("You are the Planner agent"))
            return """
                {"steps":[
                  {"order":1,"agent":"TriageAgent","purpose":"Classify the ticket","expectedOutput":"TriageResult"},
                  {"order":2,"agent":"SolutionAgent","purpose":"Find knowledge and steps","expectedOutput":"SolutionResult"},
                  {"order":3,"agent":"AssignmentAgent","purpose":"Recommend an owner","expectedOutput":"AssignmentResult"},
                  {"order":4,"agent":"ValidationAgent","purpose":"Validate and decide escalation","expectedOutput":"ValidationResult"}],
                 "rationale":"Classify first, then research and route, then validate."}
                """;

        if (systemPrompt.Contains("You are the Triage agent"))
        {
            var category = FirstCategory(userContent);
            return $$"""
                {"category":"{{category}}","priority":"High","urgencyScore":4,
                 "extractedEntities":["VPN client"],"keywords":["vpn","connection","login"],
                 "reason":"The requester is blocked from working, which maps to High priority."}
                """;
        }

        if (systemPrompt.Contains("You are the Solution agent"))
        {
            var ids = ExtractIds(userContent, "\"articleId\":");
            var idList = ids.Count > 0 ? string.Join(",", ids.Take(2)) : "";
            return $$"""
                {"matchedArticleIds":[{{idList}}],"confidence":0.75,
                 "recommendedSteps":["Verify the user's credentials","Restart the VPN client","Re-run the connection test"],
                 "summary":"Likely a stale VPN session; the retrieved articles cover the standard reset."}
                """;
        }

        if (systemPrompt.Contains("You are the Assignment agent"))
        {
            var ids = ExtractIds(userContent, "\"userId\":");
            var top = ids.Count > 0 ? ids[0] : 0;
            return $$"""
                {"recommendedAgentUserId":{{top}},"score":40.0,"alternatives":[],
                 "reason":"Highest system-computed score for this category given current workload."}
                """;
        }

        if (systemPrompt.Contains("You are the Validation and Escalation agent"))
        {
            var sla = userContent.Contains("\"slaState\":\"Breached\"") ? "Breached"
                : userContent.Contains("\"slaState\":\"AtRisk\"") ? "AtRisk"
                : userContent.Contains("\"slaState\":\"NotApplicable\"") ? "NotApplicable"
                : "OnTrack";
            var escalate = sla is "Breached" or "AtRisk" ? "true" : "false";
            return $$"""
                {"isValid":true,"violations":[],"slaRisk":"{{sla}}","requiresEscalation":{{escalate}},
                 "reason":"SLA state is {{sla}}; escalation flag set accordingly."}
                """;
        }

        throw new InvalidOperationException("ScriptedLlmClient received an unrecognised system prompt.");
    }

    private static string FirstCategory(string userContent)
    {
        const string marker = "Valid categories:";
        var i = userContent.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "General";

        var line = userContent[(i + marker.Length)..].Split('\n')[0];
        var first = line.Split(',')[0].Trim();
        return string.IsNullOrWhiteSpace(first) ? "General" : first;
    }

    private static List<int> ExtractIds(string content, string marker)
    {
        var ids = new List<int>();
        var index = 0;
        while ((index = content.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            index += marker.Length;
            var digits = new string(content[index..].TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var id) && !ids.Contains(id)) ids.Add(id);
        }
        return ids;
    }
}
