using System.Text.Json;
using System.Text.RegularExpressions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Layer 1 of validation (spec section 9.7): does the model's response even parse into the agent's
/// declared output contract, with every value inside its declared range?
///
/// Unknown JSON members are rejected rather than ignored, so an agent cannot smuggle extra fields
/// into persisted state. Failures raise AgentValidationException, which the orchestrator turns into
/// one bounded retry and then a safe failure.
/// </summary>
public static partial class AgentOutputValidator
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    [GeneratedRegex(@"^\s*```(?:json)?\s*|\s*```\s*$", RegexOptions.Multiline)]
    private static partial Regex CodeFencePattern();

    /// <summary>Models often wrap JSON in a markdown fence; strip it before parsing.</summary>
    public static string StripFences(string raw) => CodeFencePattern().Replace(raw ?? string.Empty, string.Empty).Trim();

    public static T Parse<T>(string raw, string agentName)
    {
        var json = StripFences(raw);
        if (string.IsNullOrWhiteSpace(json))
            throw new AgentValidationException($"{agentName} returned an empty response.");

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(json, Options);
            if (parsed is null)
                throw new AgentValidationException($"{agentName} returned JSON null.");
            return parsed;
        }
        catch (JsonException ex)
        {
            throw new AgentValidationException($"{agentName} returned invalid JSON: {ex.Message}");
        }
    }

    // ---- Per-contract semantic checks -------------------------------------------------

    public static PlanResult ValidatePlan(PlanResult plan)
    {
        if (plan.Steps.Count == 0)
            throw new AgentValidationException("Plan contains no steps.");
        if (plan.Steps.Count > AgentNames.Schedulable.Length)
            throw new AgentValidationException($"Plan contains {plan.Steps.Count} steps; at most {AgentNames.Schedulable.Length} are allowed.");

        foreach (var step in plan.Steps)
        {
            // A plan may only schedule agents that actually exist. No inventing new roles.
            if (!AgentNames.Schedulable.Contains(step.Agent, StringComparer.Ordinal))
                throw new AgentValidationException($"Plan names unknown agent '{step.Agent}'.");
        }

        if (plan.Steps.Select(s => s.Agent).Distinct(StringComparer.Ordinal).Count() != plan.Steps.Count)
            throw new AgentValidationException("Plan schedules the same agent more than once.");

        // Renumber defensively so persisted order is always 1..n regardless of what the model emitted.
        var ordered = plan.Steps
            .OrderBy(s => s.Order)
            .Select((s, i) => s with { Order = i + 1 })
            .ToList();

        return plan with { Steps = ordered };
    }

    public static TriageResult ValidateTriage(TriageResult r)
    {
        if (string.IsNullOrWhiteSpace(r.Category))
            throw new AgentValidationException("Triage returned an empty category.");
        if (!Enum.TryParse<TicketPriority>(r.Priority, ignoreCase: true, out _))
            throw new AgentValidationException($"Triage returned invalid priority '{r.Priority}'.");
        if (r.UrgencyScore is < 1 or > 5)
            throw new AgentValidationException($"Triage urgencyScore {r.UrgencyScore} is outside 1-5.");
        if (string.IsNullOrWhiteSpace(r.Reason))
            throw new AgentValidationException("Triage returned no reason.");
        return r;
    }

    /// <summary>
    /// Validates the Solution output and drops any article id the SearchKnowledgeBase tool did not return,
    /// so the agent cannot attach an article it never actually retrieved.
    /// </summary>
    public static SolutionResult ValidateSolution(SolutionResult r, IReadOnlyCollection<int> retrievedArticleIds)
    {
        if (r.Confidence is < 0 or > 1)
            throw new AgentValidationException($"Solution confidence {r.Confidence} is outside 0.0-1.0.");
        if (r.RecommendedSteps.Count == 0)
            throw new AgentValidationException("Solution returned no recommended steps.");

        var filtered = r.MatchedArticleIds.Where(retrievedArticleIds.Contains).Distinct().ToList();
        return r with { MatchedArticleIds = filtered };
    }

    /// <summary>Validates the Assignment output against the candidate list the tools actually produced.</summary>
    public static AssignmentResult ValidateAssignment(AssignmentResult r, IReadOnlyCollection<int> candidateUserIds)
    {
        if (r.RecommendedAgentUserId <= 0)
            throw new AgentValidationException("Assignment returned no recommended agent.");
        if (!candidateUserIds.Contains(r.RecommendedAgentUserId))
            throw new AgentValidationException(
                $"Assignment recommended user {r.RecommendedAgentUserId}, who was not among the scored candidates.");
        if (string.IsNullOrWhiteSpace(r.Reason))
            throw new AgentValidationException("Assignment returned no reason.");

        var alternatives = r.Alternatives.Where(a => candidateUserIds.Contains(a.UserId)).ToList();
        return r with { Alternatives = alternatives };
    }

    public static ValidationResult ValidateValidation(ValidationResult r)
    {
        if (!Enum.TryParse<SlaState>(r.SlaRisk, ignoreCase: true, out _))
            throw new AgentValidationException($"Validation returned invalid slaRisk '{r.SlaRisk}'.");
        if (string.IsNullOrWhiteSpace(r.Reason))
            throw new AgentValidationException("Validation returned no reason.");
        return r;
    }
}
