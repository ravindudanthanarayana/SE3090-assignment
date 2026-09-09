using System.ComponentModel.DataAnnotations;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Dtos;

public sealed class StartWorkflowRequest
{
    [Range(1, int.MaxValue)]
    public int TicketId { get; set; }

    [MaxLength(500)]
    public string? Objective { get; set; }
}

public sealed class WorkflowQuery : PagedQuery
{
    public WorkflowStatus? Status { get; set; }
    public int? TicketId { get; set; }
}

public sealed record WorkflowListItemDto(
    int Id,
    int TicketId,
    string TicketNumber,
    string Objective,
    WorkflowStatus Status,
    string? CurrentStep,
    int StepCount,
    int PendingApprovals,
    DateTime StartedAt,
    DateTime? CompletedAt);

/// <summary>
/// The full auditable execution summary (spec section 9.9): plan, every agent step with its timing,
/// retry count and validation result, every tool call, the approval decision and the final outcome.
/// </summary>
public sealed record WorkflowDetailDto(
    int Id,
    int TicketId,
    string TicketNumber,
    string TicketTitle,
    string Objective,
    WorkflowStatus Status,
    string? CurrentStep,
    string? PlanJson,
    string? FinalOutcomeJson,
    string? ErrorMessage,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalDurationMs,
    IReadOnlyList<AgentStepDto> Steps,
    IReadOnlyList<ApprovalDto> Approvals,
    /// <summary>Tool calls the orchestrator made outside any agent step, such as requesting approval.</summary>
    IReadOnlyList<ToolCallDto> WorkflowToolCalls);

public sealed record AgentStepDto(
    int Id,
    int StepOrder,
    string AgentName,
    string? Purpose,
    AgentStepStatus Status,
    string? InputJson,
    string? OutputJson,
    string? ValidationJson,
    string? ErrorMessage,
    int RetryCount,
    int DurationMs,
    DateTime StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<ToolCallDto> ToolCalls);

public sealed record ToolCallDto(
    int Id,
    string ToolName,
    string? InputJson,
    string? OutputJson,
    bool Success,
    string? ErrorMessage,
    int DurationMs,
    DateTime CreatedAt);

public sealed record ApprovalDto(
    int Id,
    int WorkflowId,
    int TicketId,
    string TicketNumber,
    string TicketTitle,
    ApprovalActionType ActionType,
    string ProposedActionJson,
    string Reason,
    RiskLevel RiskLevel,
    ApprovalStatus Status,
    DateTime RequestedAt,
    string? DecidedByName,
    DateTime? DecidedAt,
    string? DecisionNote);

public sealed class ApprovalDecisionRequest
{
    /// <summary>Approve, Reject or RevisionRequested. Pending is not a valid decision.</summary>
    [Required]
    public ApprovalStatus Decision { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public sealed class ApprovalQuery : PagedQuery
{
    public ApprovalStatus? Status { get; set; }
}

public sealed record AgentToolInfoDto(string Name, string Description, IReadOnlyList<string> AllowedForAgents);
