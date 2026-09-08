using System.Text.Json.Serialization;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Agents.Contracts;

/// <summary>
/// Canonical agent names. Used for the plan, the allow-list lookup and the persisted AgentStep rows,
/// so a typo cannot silently create a fifth "agent".
/// </summary>
public static class AgentNames
{
    public const string Planner = "PlannerAgent";
    public const string Triage = "TriageAgent";
    public const string Solution = "SolutionAgent";
    public const string Assignment = "AssignmentAgent";
    public const string Validation = "ValidationAgent";

    /// <summary>The specialists the Planner is allowed to schedule. Planner itself is never a plan step.</summary>
    public static readonly string[] Schedulable = [Triage, Solution, Assignment, Validation];
}

// ---------------------------------------------------------------------------
// Agent 0 - Planner / Coordinator  (group-owned)
// ---------------------------------------------------------------------------

public sealed record PlanStep
{
    [JsonPropertyName("order")] public int Order { get; init; }
    [JsonPropertyName("agent")] public string Agent { get; init; } = string.Empty;
    [JsonPropertyName("purpose")] public string Purpose { get; init; } = string.Empty;
    [JsonPropertyName("expectedOutput")] public string ExpectedOutput { get; init; } = string.Empty;
}

public sealed record PlanResult
{
    [JsonPropertyName("steps")] public List<PlanStep> Steps { get; init; } = [];
    [JsonPropertyName("rationale")] public string Rationale { get; init; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Agent 1 - Triage  (Student 1, Component A)
// ---------------------------------------------------------------------------

public sealed record TriageResult
{
    /// <summary>Category name. Validated against TicketCategories; unknown values fall back to General.</summary>
    [JsonPropertyName("category")] public string Category { get; init; } = string.Empty;

    /// <summary>Low | Medium | High | Critical. Validated against the TicketPriority enum.</summary>
    [JsonPropertyName("priority")] public string Priority { get; init; } = string.Empty;

    /// <summary>1-5. Bounds-checked by the deterministic validator.</summary>
    [JsonPropertyName("urgencyScore")] public int UrgencyScore { get; init; }

    [JsonPropertyName("extractedEntities")] public List<string> ExtractedEntities { get; init; } = [];
    [JsonPropertyName("keywords")] public List<string> Keywords { get; init; } = [];
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Agent 2 - Solution / Knowledge  (Student 3, Component C)
// ---------------------------------------------------------------------------

public sealed record SolutionResult
{
    /// <summary>Ids returned by the SearchKnowledgeBase tool. Any id not seen from the tool is discarded.</summary>
    [JsonPropertyName("matchedArticleIds")] public List<int> MatchedArticleIds { get; init; } = [];

    /// <summary>0.0-1.0, bounds-checked.</summary>
    [JsonPropertyName("confidence")] public double Confidence { get; init; }

    [JsonPropertyName("recommendedSteps")] public List<string> RecommendedSteps { get; init; } = [];
    [JsonPropertyName("summary")] public string Summary { get; init; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Agent 3 - Assignment  (Student 2, Component B)
// ---------------------------------------------------------------------------

public sealed record AssignmentAlternative
{
    [JsonPropertyName("userId")] public int UserId { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}

public sealed record AssignmentResult
{
    /// <summary>Must be an active SupportAgent returned by the GetSupportAgents tool, or the output is rejected.</summary>
    [JsonPropertyName("recommendedAgentUserId")] public int RecommendedAgentUserId { get; init; }

    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("alternatives")] public List<AssignmentAlternative> Alternatives { get; init; } = [];
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Agent 4 - Validation and Escalation  (Student 4, Component D)
// ---------------------------------------------------------------------------

public sealed record ValidationResult
{
    [JsonPropertyName("isValid")] public bool IsValid { get; init; }
    [JsonPropertyName("violations")] public List<string> Violations { get; init; } = [];

    /// <summary>OnTrack | AtRisk | Breached | NotApplicable. Cross-checked against the CheckSla tool result.</summary>
    [JsonPropertyName("slaRisk")] public string SlaRisk { get; init; } = string.Empty;

    [JsonPropertyName("requiresEscalation")] public bool RequiresEscalation { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Combined, post-validation workflow outcome persisted to AgentWorkflow.FinalOutcomeJson
// ---------------------------------------------------------------------------

public sealed record WorkflowOutcome
{
    [JsonPropertyName("triage")] public TriageResult? Triage { get; init; }
    [JsonPropertyName("solution")] public SolutionResult? Solution { get; init; }
    [JsonPropertyName("assignment")] public AssignmentResult? Assignment { get; init; }
    [JsonPropertyName("validation")] public ValidationResult? Validation { get; init; }

    /// <summary>Changes the backend actually applied without needing approval.</summary>
    [JsonPropertyName("appliedActions")] public List<string> AppliedActions { get; init; } = [];

    /// <summary>Actions held back for a human decision.</summary>
    [JsonPropertyName("pendingApprovalActions")] public List<string> PendingApprovalActions { get; init; } = [];

    [JsonPropertyName("summary")] public string Summary { get; init; } = string.Empty;
}

/// <summary>The concrete, schema-checked action attached to an AiApproval row.</summary>
public sealed record ProposedAction
{
    [JsonPropertyName("actionType")] public ApprovalActionType ActionType { get; init; }
    [JsonPropertyName("ticketId")] public int TicketId { get; init; }
    [JsonPropertyName("targetUserId")] public int? TargetUserId { get; init; }
    [JsonPropertyName("targetPriority")] public TicketPriority? TargetPriority { get; init; }
    [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;
}
