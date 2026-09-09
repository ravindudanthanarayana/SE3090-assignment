using System.Text.Json;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Agents.Prompts;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.Common;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Agent 2 - Solution / Knowledge. Owned by Student 3 (Component C, Knowledge Base).
/// Responsibility: ground the ticket in real knowledge-base content and propose troubleshooting steps.
/// Input: ticket plus the Triage classification. Output: SolutionResult.
/// Tools: GetTicket, SearchKnowledgeBase. It cannot see support agents or workload.
/// </summary>
public sealed class SolutionAgent(ILlmClient llm, ToolRegistry tools) : IWorkflowAgent
{
    public string Name => AgentNames.Solution;
    public IReadOnlyCollection<string> AllowedTools { get; } = ["GetTicket", "SearchKnowledgeBase"];

    public async Task<(object Input, object Output)> ExecuteAsync(AgentRunContext ctx, CancellationToken ct = default)
    {
        var triage = ctx.Scratchpad.Triage;

        // Build the search query from Triage's keywords, falling back to the ticket title.
        var keywords = triage?.Keywords is { Count: > 0 }
            ? string.Join(" ", triage.Keywords)
            : ctx.Ticket.Title;

        var search = await tools.InvokeAsync(
            Name, AllowedTools, "SearchKnowledgeBase",
            new { query = keywords, categoryId = ctx.Ticket.CategoryId }, ctx.Tool, ctx.StepId, ct);

        if (!search.Success)
            throw new AgentValidationException($"SearchKnowledgeBase failed: {search.Error}");

        // Record which article ids were genuinely retrieved so the validator can reject any others.
        var retrieved = ExtractArticleIds(search.Data);
        ctx.Scratchpad.RetrievedArticleIds.Clear();
        ctx.Scratchpad.RetrievedArticleIds.AddRange(retrieved);

        var input = new
        {
            ticketId = ctx.Ticket.Id,
            triageCategory = triage?.Category,
            searchQuery = keywords,
            retrievedArticleIds = retrieved
        };

        var userContent = $"""
            Triage classification: category={triage?.Category ?? "unknown"}, priority={triage?.Priority ?? "unknown"}

            {PromptSanitizer.WrapUntrusted("ticketTitle", ctx.Ticket.Title)}

            {PromptSanitizer.WrapUntrusted("ticketDescription", ctx.Ticket.Description)}

            Knowledge base search results (the only article ids you may cite):
            {JsonSerializer.Serialize(search.Data)}

            Propose how to resolve this ticket.
            """;

        var raw = await llm.CompleteJsonAsync(SystemPrompts.Solution, userContent, ct);
        var parsed = AgentOutputValidator.Parse<SolutionResult>(raw, Name);
        var validated = AgentOutputValidator.ValidateSolution(parsed, retrieved);

        ctx.Scratchpad.Solution = validated;
        return (input, validated);
    }

    private static List<int> ExtractArticleIds(object? data)
    {
        var ids = new List<int>();
        if (data is null) return ids;

        var element = JsonSerializer.SerializeToElement(data);
        if (element.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in results.EnumerateArray())
            {
                if (r.TryGetProperty("articleId", out var id) && id.ValueKind == JsonValueKind.Number)
                    ids.Add(id.GetInt32());
            }
        }
        return ids;
    }
}
