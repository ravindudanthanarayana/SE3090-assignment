using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.BusinessRules;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Tests.Unit;

/// <summary>
/// The gate that decides what agent advice is actually allowed to do.
/// Every case here is a business rule the spec requires to sit outside the language model.
/// </summary>
public class BusinessRuleEngineTests
{
    private static readonly Dictionary<string, int> Categories =
        new(StringComparer.OrdinalIgnoreCase) { ["Network"] = 1, ["Hardware"] = 2, ["General"] = 3 };

    private static readonly int[] ActiveAgents = [2, 3];

    private static Ticket Ticket(
        TicketPriority priority = TicketPriority.Medium,
        int categoryId = 1,
        int? assignedTo = null) => new()
    {
        Id = 10,
        TicketNumber = "TKT-000010",
        CategoryId = categoryId,
        Priority = priority,
        Status = TicketStatus.New,
        AssignedToUserId = assignedTo,
        Category = new TicketCategory { Id = categoryId, Name = "Network", DefaultSlaHours = 8 }
    };

    private static RuleDecision Evaluate(
        Ticket ticket,
        TriageResult? triage = null,
        AssignmentResult? assignment = null,
        ValidationResult? validation = null,
        SlaState sla = SlaState.OnTrack)
        => BusinessRuleEngine.Evaluate(ticket, triage, assignment, validation, Categories, ActiveAgents, sla);

    [Fact]
    public void Ai_may_raise_priority_without_asking()
    {
        var decision = Evaluate(
            Ticket(TicketPriority.Low),
            new TriageResult { Category = "Network", Priority = "High", UrgencyScore = 4, Reason = "User blocked" });

        Assert.Equal(TicketPriority.High, decision.ApplyPriority);
        Assert.DoesNotContain(decision.RequiresApproval, a => a.ActionType == ApprovalActionType.ChangePriority);
    }

    [Fact]
    public void Lowering_priority_requires_human_approval()
    {
        var decision = Evaluate(
            Ticket(TicketPriority.High),
            new TriageResult { Category = "Network", Priority = "Low", UrgencyScore = 1, Reason = "Looks cosmetic" });

        Assert.Null(decision.ApplyPriority);
        var action = Assert.Single(decision.RequiresApproval, a => a.ActionType == ApprovalActionType.ChangePriority);
        Assert.Equal(TicketPriority.Low, action.TargetPriority);
    }

    [Fact]
    public void A_category_the_agent_invented_is_rejected_and_the_ticket_keeps_its_own()
    {
        var decision = Evaluate(
            Ticket(),
            new TriageResult { Category = "Quantum Networking", Priority = "Medium", UrgencyScore = 3, Reason = "x" });

        Assert.Null(decision.ApplyCategoryId);
        Assert.Contains(decision.Violations, v => v.Rule == "UnknownCategory");
    }

    [Fact]
    public void An_invalid_priority_value_is_recorded_as_a_violation()
    {
        var decision = Evaluate(
            Ticket(),
            new TriageResult { Category = "Network", Priority = "Catastrophic", UrgencyScore = 5, Reason = "x" });

        Assert.Contains(decision.Violations, v => v.Rule == "InvalidPriority");
        Assert.Null(decision.ApplyPriority);
    }

    [Fact]
    public void Recommending_a_user_who_is_not_an_active_support_agent_is_rejected()
    {
        var decision = Evaluate(
            Ticket(),
            assignment: new AssignmentResult { RecommendedAgentUserId = 99, Reason = "Trust me" });

        Assert.Contains(decision.Violations, v => v.Rule == "InvalidAssignee");
        Assert.Empty(decision.RequiresApproval);
    }

    [Fact]
    public void Assignment_always_requires_approval_the_ai_never_assigns_directly()
    {
        var decision = Evaluate(
            Ticket(TicketPriority.Low),
            assignment: new AssignmentResult { RecommendedAgentUserId = 2, Reason = "Best fit" });

        var action = Assert.Single(decision.RequiresApproval, a => a.ActionType == ApprovalActionType.Assign);
        Assert.Equal(2, action.TargetUserId);
    }

    [Fact]
    public void Recommending_the_agent_who_already_owns_the_ticket_is_a_no_op()
    {
        var decision = Evaluate(
            Ticket(assignedTo: 2),
            assignment: new AssignmentResult { RecommendedAgentUserId = 2, Reason = "Already theirs" });

        Assert.Empty(decision.RequiresApproval);
    }

    [Fact]
    public void Escalation_always_requires_approval()
    {
        var decision = Evaluate(
            Ticket(),
            validation: new ValidationResult
            {
                IsValid = true, SlaRisk = "AtRisk", RequiresEscalation = true, Reason = "SLA at risk"
            },
            sla: SlaState.AtRisk);

        Assert.Single(decision.RequiresApproval, a => a.ActionType == ApprovalActionType.Escalate);
    }

    [Fact]
    public void Our_own_sla_calculation_overrides_the_agents_claim()
    {
        var decision = Evaluate(
            Ticket(),
            validation: new ValidationResult
            {
                IsValid = true, SlaRisk = "OnTrack", RequiresEscalation = false, Reason = "Looks fine"
            },
            sla: SlaState.Breached);

        Assert.Contains(decision.Violations, v => v.Rule == "SlaMismatch");
    }

    [Fact]
    public void A_critical_ticket_can_never_be_auto_closed()
    {
        var decision = Evaluate(Ticket(TicketPriority.Critical));
        Assert.Contains(decision.Violations, v => v.Rule == "CriticalNoAutoClose");
    }

    [Fact]
    public void No_agent_output_at_all_still_produces_a_safe_decision()
    {
        var decision = Evaluate(Ticket());
        Assert.True(decision.Accepted);
        Assert.Empty(decision.RequiresApproval);
    }
}
