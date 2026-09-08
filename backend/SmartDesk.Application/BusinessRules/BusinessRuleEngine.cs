using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.BusinessRules;

public sealed record RuleViolation(string Rule, string Message);

/// <summary>
/// The deterministic gate between what the agents *suggest* and what the system *does* (spec section 9.7).
/// Agent output is advice; these rules decide. Nothing here calls an LLM.
/// </summary>
public sealed record RuleDecision
{
    public required bool Accepted { get; init; }
    public List<RuleViolation> Violations { get; init; } = [];

    /// <summary>Safe, low-impact changes the backend may apply immediately.</summary>
    public TicketPriority? ApplyPriority { get; init; }
    public int? ApplyCategoryId { get; init; }

    /// <summary>High-impact actions that must pause for a human decision.</summary>
    public List<ProposedAction> RequiresApproval { get; init; } = [];

    public string Summary { get; init; } = string.Empty;
}

public static class BusinessRuleEngine
{
    /// <summary>
    /// Evaluates the combined agent output against the real database state.
    /// Every identifier the agents produced is re-checked here against the entities passed in -
    /// the agents' claims are never trusted.
    /// </summary>
    public static RuleDecision Evaluate(
        Ticket ticket,
        TriageResult? triage,
        AssignmentResult? assignment,
        ValidationResult? validation,
        IReadOnlyDictionary<string, int> categoriesByName,
        IReadOnlyCollection<int> activeSupportAgentIds,
        SlaState actualSlaState)
    {
        var violations = new List<RuleViolation>();
        var approvals = new List<ProposedAction>();
        TicketPriority? applyPriority = null;
        int? applyCategoryId = null;

        // --- Rule 1: a proposed category must actually exist. Unknown values are ignored, not invented.
        if (triage is not null && !string.IsNullOrWhiteSpace(triage.Category))
        {
            if (categoriesByName.TryGetValue(triage.Category, out var categoryId))
            {
                if (categoryId != ticket.CategoryId) applyCategoryId = categoryId;
            }
            else
            {
                violations.Add(new RuleViolation(
                    "UnknownCategory",
                    $"Agent proposed category '{triage.Category}', which does not exist. Keeping the current category."));
            }
        }

        // --- Rule 2: the AI may raise priority on its own, but lowering it needs a human.
        if (triage is not null && Enum.TryParse<TicketPriority>(triage.Priority, ignoreCase: true, out var proposed))
        {
            if (proposed > ticket.Priority)
            {
                applyPriority = proposed;
            }
            else if (proposed < ticket.Priority)
            {
                approvals.Add(new ProposedAction
                {
                    ActionType = ApprovalActionType.ChangePriority,
                    TicketId = ticket.Id,
                    TargetPriority = proposed,
                    Description = $"Lower priority from {ticket.Priority} to {proposed}: {triage.Reason}"
                });
            }
        }
        else if (triage is not null)
        {
            violations.Add(new RuleViolation(
                "InvalidPriority",
                $"Agent proposed priority '{triage.Priority}', which is not a valid value."));
        }

        // --- Rule 3: a recommended assignee must be an active SupportAgent that exists right now.
        if (assignment is not null && assignment.RecommendedAgentUserId > 0)
        {
            if (!activeSupportAgentIds.Contains(assignment.RecommendedAgentUserId))
            {
                violations.Add(new RuleViolation(
                    "InvalidAssignee",
                    $"Agent recommended user {assignment.RecommendedAgentUserId}, who is not an active support agent."));
            }
            else if (assignment.RecommendedAgentUserId != ticket.AssignedToUserId)
            {
                // Rule 4: assignment transfers ownership of work to a named person, so it is always
                // treated as high-impact and always requires a manager decision. The AI never assigns.
                approvals.Add(new ProposedAction
                {
                    ActionType = ApprovalActionType.Assign,
                    TicketId = ticket.Id,
                    TargetUserId = assignment.RecommendedAgentUserId,
                    Description = $"Assign to user {assignment.RecommendedAgentUserId}: {assignment.Reason}"
                });
            }
        }

        // --- Rule 5: escalation is always high-impact and always requires approval. Never automatic.
        if (validation?.RequiresEscalation == true)
        {
            approvals.Add(new ProposedAction
            {
                ActionType = ApprovalActionType.Escalate,
                TicketId = ticket.Id,
                Description = $"Escalate ticket {ticket.TicketNumber}: {validation.Reason}"
            });
        }

        // --- Rule 6: the agent's SLA claim must match our own calculation. Ours wins.
        if (validation is not null &&
            Enum.TryParse<SlaState>(validation.SlaRisk, ignoreCase: true, out var claimed) &&
            claimed != actualSlaState)
        {
            violations.Add(new RuleViolation(
                "SlaMismatch",
                $"Agent reported SLA state '{claimed}' but the calculated state is '{actualSlaState}'. Using the calculated value."));
        }

        // --- Rule 7: a Critical ticket may never be auto-resolved or auto-closed by the AI.
        if (ticket.Priority == TicketPriority.Critical || applyPriority == TicketPriority.Critical)
        {
            violations.Add(new RuleViolation(
                "CriticalNoAutoClose",
                "Critical tickets cannot be automatically resolved or closed; a human must act."));
        }

        var summary = approvals.Count > 0
            ? $"{approvals.Count} high-impact action(s) held for human approval."
            : "No high-impact action proposed; safe updates applied automatically.";

        return new RuleDecision
        {
            // Violations are advisory: we degrade safely rather than discarding the whole workflow.
            Accepted = true,
            Violations = violations,
            ApplyPriority = applyPriority,
            ApplyCategoryId = applyCategoryId,
            RequiresApproval = approvals,
            Summary = summary
        };
    }
}
