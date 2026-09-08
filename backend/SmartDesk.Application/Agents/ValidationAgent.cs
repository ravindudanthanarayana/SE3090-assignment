using System.Text.Json;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Agents.Prompts;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.Common;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Agent 4 - Validation and Escalation. Owned by Student 4 (Component D, SLA/Escalation/Reporting).
/// Responsibility: the safety and consistency check over the other agents, plus the escalation decision.
/// Input: ticket, all prior agent outputs, and the system's own SLA facts.
/// Output: ValidationResult. Its escalation decision is a recommendation that must be approved.
/// Tools: CheckSla only - it reads facts, it does not gather new opinions.
/// </summary>
public sealed class ValidationAgent(ILlmClient llm, ToolRegistry tools) : IWorkflowAgent
{
    public string Name => AgentNames.Validation;
    public IReadOnlyCollection<string> AllowedTools { get; } = ["CheckSla"];

    public async Task<(object Input, object Output)> ExecuteAsync(AgentRunContext ctx, CancellationToken ct = default)
    {
        var sla = await tools.InvokeAsync(
            Name, AllowedTools, "CheckSla", new { ticketId = ctx.Tool.TicketId }, ctx.Tool, ctx.StepId, ct);

        if (!sla.Success)
            throw new AgentValidationException($"CheckSla failed: {sla.Error}");

        var pad = ctx.Scratchpad;
        var input = new
        {
            ticketId = ctx.Ticket.Id,
            triage = pad.Triage,
            solution = pad.Solution,
            assignment = pad.Assignment,
            slaFacts = sla.Data
        };

        var userContent = $"""
            SLA facts computed by the system (authoritative - do not estimate these yourself):
            {JsonSerializer.Serialize(sla.Data)}

            Triage result:    {JsonSerializer.Serialize(pad.Triage)}
            Solution result:  {JsonSerializer.Serialize(pad.Solution)}
            Assignment result:{JsonSerializer.Serialize(pad.Assignment)}

            Validate the above and decide whether this ticket requires escalation.
            """;

        var raw = await llm.CompleteJsonAsync(SystemPrompts.Validation, userContent, ct);
        var parsed = AgentOutputValidator.Parse<ValidationResult>(raw, Name);
        var validated = AgentOutputValidator.ValidateValidation(parsed);

        ctx.Scratchpad.Validation = validated;
        return (input, validated);
    }
}
