using System.Text.Json;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Agents.Prompts;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.Common;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Agent 3 - Assignment. Owned by Student 2 (Component B, Assignment and Workload).
/// Responsibility: recommend which support agent should own the ticket.
/// Input: ticket, Triage classification, and the system-computed candidate ranking.
/// Output: AssignmentResult. It recommends only - assignment always needs manager approval.
/// Tools: GetSupportAgents, GetAgentWorkload, ScoreAssignmentCandidates.
/// </summary>
public sealed class AssignmentAgent(ILlmClient llm, ToolRegistry tools) : IWorkflowAgent
{
    public string Name => AgentNames.Assignment;
    public IReadOnlyCollection<string> AllowedTools { get; } =
        ["GetSupportAgents", "GetAgentWorkload", "ScoreAssignmentCandidates"];

    public async Task<(object Input, object Output)> ExecuteAsync(AgentRunContext ctx, CancellationToken ct = default)
    {
        var agents = await tools.InvokeAsync(
            Name, AllowedTools, "GetSupportAgents", new { }, ctx.Tool, ctx.StepId, ct);
        if (!agents.Success)
            throw new AgentValidationException($"GetSupportAgents failed: {agents.Error}");

        var workload = await tools.InvokeAsync(
            Name, AllowedTools, "GetAgentWorkload", new { }, ctx.Tool, ctx.StepId, ct);
        if (!workload.Success)
            throw new AgentValidationException($"GetAgentWorkload failed: {workload.Error}");

        var scored = await tools.InvokeAsync(
            Name, AllowedTools, "ScoreAssignmentCandidates",
            new { categoryId = ctx.Ticket.CategoryId }, ctx.Tool, ctx.StepId, ct);
        if (!scored.Success)
            throw new AgentValidationException($"ScoreAssignmentCandidates failed: {scored.Error}");

        var candidateIds = ExtractUserIds(scored.Data);
        if (candidateIds.Count == 0)
            throw new AgentValidationException("No active support agents are available to assign.");

        ctx.Scratchpad.ScoredCandidateUserIds.Clear();
        ctx.Scratchpad.ScoredCandidateUserIds.AddRange(candidateIds);

        var input = new
        {
            ticketId = ctx.Ticket.Id,
            triageCategory = ctx.Scratchpad.Triage?.Category,
            candidateUserIds = candidateIds
        };

        var userContent = $"""
            Ticket category id: {ctx.Ticket.CategoryId}
            Triage priority: {ctx.Scratchpad.Triage?.Priority ?? ctx.Ticket.Priority.ToString()}

            System-computed candidate ranking (you must choose a userId from this list):
            {JsonSerializer.Serialize(scored.Data)}

            Current workload:
            {JsonSerializer.Serialize(workload.Data)}

            Recommend the support agent who should handle this ticket.
            """;

        var raw = await llm.CompleteJsonAsync(SystemPrompts.Assignment, userContent, ct);
        var parsed = AgentOutputValidator.Parse<AssignmentResult>(raw, Name);
        var validated = AgentOutputValidator.ValidateAssignment(parsed, candidateIds);

        ctx.Scratchpad.Assignment = validated;
        return (input, validated);
    }

    private static List<int> ExtractUserIds(object? data)
    {
        var ids = new List<int>();
        if (data is null) return ids;

        var element = JsonSerializer.SerializeToElement(data);
        if (element.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in candidates.EnumerateArray())
            {
                if (c.TryGetProperty("userId", out var id) && id.ValueKind == JsonValueKind.Number)
                    ids.Add(id.GetInt32());
            }
        }
        return ids;
    }
}
