using System.Text.Json;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Agents.Prompts;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.Common;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Agent 1 - Triage. Owned by Student 1 (Component A, Ticket Management).
/// Responsibility: turn one ticket's free text into a structured classification.
/// Input: ticket title, description and the valid category list. Output: TriageResult.
/// Tools: GetTicket only - it has no reason to see agents, workload or the knowledge base.
/// </summary>
public sealed class TriageAgent(ILlmClient llm, ToolRegistry tools) : IWorkflowAgent
{
    public string Name => AgentNames.Triage;
    public IReadOnlyCollection<string> AllowedTools { get; } = ["GetTicket"];

    public async Task<(object Input, object Output)> ExecuteAsync(AgentRunContext ctx, CancellationToken ct = default)
    {
        var ticketResult = await tools.InvokeAsync(
            Name, AllowedTools, "GetTicket",
            new { ticketId = ctx.Tool.TicketId }, ctx.Tool, ctx.StepId, ct);

        if (!ticketResult.Success)
            throw new AgentValidationException($"GetTicket failed: {ticketResult.Error}");

        var input = new
        {
            ticketId = ctx.Ticket.Id,
            availableCategories = ctx.CategoryNames
        };

        var userContent = $"""
            Valid categories: {string.Join(", ", ctx.CategoryNames)}

            {PromptSanitizer.WrapUntrusted("ticketTitle", ctx.Ticket.Title)}

            {PromptSanitizer.WrapUntrusted("ticketDescription", ctx.Ticket.Description)}

            Classify this ticket.
            """;

        var raw = await llm.CompleteJsonAsync(SystemPrompts.Triage, userContent, ct);
        var parsed = AgentOutputValidator.Parse<TriageResult>(raw, Name);
        var validated = AgentOutputValidator.ValidateTriage(parsed);

        ctx.Scratchpad.Triage = validated;
        return (input, validated);
    }
}
