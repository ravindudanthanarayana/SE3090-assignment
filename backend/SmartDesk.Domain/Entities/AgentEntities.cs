using SmartDesk.Domain.Enums;

namespace SmartDesk.Domain.Entities;

/// <summary>
/// Root of the persisted agent workflow state (spec section 9.6).
/// Stores structured decisions only - never prompts, never model reasoning.
/// </summary>
public class AgentWorkflow
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>The domain objective handed to the Planner agent.</summary>
    public string Objective { get; set; } = string.Empty;

    public WorkflowStatus Status { get; set; } = WorkflowStatus.Planned;
    /// <summary>Name of the agent currently executing, or the last one that ran.</summary>
    public string? CurrentStep { get; set; }

    /// <summary>Validated PlanResult as jsonb.</summary>
    public string? PlanJson { get; set; }
    /// <summary>Validated final outcome summary as jsonb.</summary>
    public string? FinalOutcomeJson { get; set; }
    public string? ErrorMessage { get; set; }

    public int? StartedByUserId { get; set; }
    public User? StartedByUser { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<AgentStep> Steps { get; set; } = [];
    public ICollection<AgentToolCall> ToolCalls { get; set; } = [];
    public ICollection<AiApproval> Approvals { get; set; } = [];
}

/// <summary>One execution of one agent. Gives each agent visible participation (spec section 9.2).</summary>
public class AgentStep
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = null!;

    public int StepOrder { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public AgentStepStatus Status { get; set; } = AgentStepStatus.Pending;

    /// <summary>Structured input contract as jsonb (no prompt text).</summary>
    public string? InputJson { get; set; }
    /// <summary>Structured, schema-validated output contract as jsonb.</summary>
    public string? OutputJson { get; set; }
    /// <summary>Result of deterministic validation applied to the output.</summary>
    public string? ValidationJson { get; set; }

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int DurationMs { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<AgentToolCall> ToolCalls { get; set; } = [];
}

/// <summary>Observability record for every allow-listed tool invocation (spec section 9.9).</summary>
public class AgentToolCall
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = null!;
    public int? StepId { get; set; }
    public AgentStep? Step { get; set; }

    public string ToolName { get; set; } = string.Empty;
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// The human-in-the-loop gate (spec section 9.8). A row here is created by the RequestHumanApproval tool
/// and can only be decided by a SupportManager or Admin. Nothing is executed until Status = Approved.
/// </summary>
public class AiApproval
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = null!;
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public ApprovalActionType ActionType { get; set; }
    /// <summary>Schema-validated description of what would be executed, as jsonb.</summary>
    public string ProposedActionJson { get; set; } = "{}";
    public string Reason { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.High;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime RequestedAt { get; set; }
    public int? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public ActorType ActorType { get; set; } = ActorType.User;
    /// <summary>Structured detail as jsonb. Must not contain secrets or model reasoning.</summary>
    public string? DetailsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Send log for the third-party notification provider (spec section 11).</summary>
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string Channel { get; set; } = "Email";
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
