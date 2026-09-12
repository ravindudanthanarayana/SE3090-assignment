namespace SmartDesk.Domain.Enums;

/// <summary>Ticket lifecycle states. Transitions are enforced by TicketService, never by the LLM.</summary>
public enum TicketStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    OnHold = 3,
    Escalated = 4,
    Resolved = 5,
    Closed = 6,
    Cancelled = 7
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>Where an assignment came from. AiApproved means a manager approved an agent recommendation.</summary>
public enum AssignmentSource
{
    Manual = 0,
    AiApproved = 1
}

/// <summary>How a knowledge article got attached to a ticket.</summary>
public enum ArticleLinkSource
{
    Manual = 0,
    Agent = 1
}

public enum WorkflowStatus
{
    Planned = 0,
    Running = 1,
    AwaitingApproval = 2,
    Completed = 3,
    Failed = 4,
    Rejected = 5
}

public enum AgentStepStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4
}

/// <summary>High-impact actions that must pause for human approval (spec section 9.8).</summary>
public enum ApprovalActionType
{
    Escalate = 0,
    Assign = 1,
    ChangePriority = 2
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    RevisionRequested = 3
}

public enum RiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>Who performed an audited action.</summary>
public enum ActorType
{
    User = 0,
    System = 1,
    Agent = 2
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

/// <summary>Derived SLA position of a ticket. Computed in C#, never by the LLM.</summary>
public enum SlaState
{
    OnTrack = 0,
    AtRisk = 1,
    Breached = 2,
    NotApplicable = 3
}
