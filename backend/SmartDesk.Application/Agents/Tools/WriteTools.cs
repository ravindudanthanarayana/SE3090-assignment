using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Agents.Tools;

/// <summary>
/// The only tool that writes anything, and it deliberately writes the weakest possible thing:
/// a Pending approval request. It never changes a ticket. Executing the action requires a separate,
/// authenticated decision by a SupportManager or Admin (spec section 9.8).
/// </summary>
public sealed class RequestHumanApprovalTool(IAppDbContext db, IClock clock) : IAgentTool
{
    public string Name => "RequestHumanApproval";
    public string Description => "Creates a pending approval request for a high-impact action. Executes nothing.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        // --- Input validation: the action type must be one of the three we support.
        if (!input.TryGetProperty("actionType", out var at) || at.ValueKind != JsonValueKind.String ||
            !Enum.TryParse<ApprovalActionType>(at.GetString(), ignoreCase: true, out var actionType))
        {
            return ToolResult.Fail("'actionType' must be one of: Escalate, Assign, ChangePriority.");
        }

        if (!input.TryGetProperty("reason", out var r) || r.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(r.GetString()))
        {
            return ToolResult.Fail("'reason' is required.");
        }

        var reason = r.GetString()!.Trim();
        if (reason.Length > 1000) reason = reason[..1000];

        var action = new ProposedAction
        {
            ActionType = actionType,
            // The ticket always comes from the workflow context, never from agent input.
            TicketId = context.TicketId,
            TargetUserId = input.TryGetProperty("targetUserId", out var u) && u.ValueKind == JsonValueKind.Number
                ? u.GetInt32() : null,
            TargetPriority = input.TryGetProperty("targetPriority", out var p) && p.ValueKind == JsonValueKind.String &&
                             Enum.TryParse<TicketPriority>(p.GetString(), ignoreCase: true, out var pr)
                ? pr : null,
            Description = reason
        };

        // Assignment requests must name a real, active support agent.
        if (actionType == ApprovalActionType.Assign)
        {
            if (action.TargetUserId is null)
                return ToolResult.Fail("'targetUserId' is required for an Assign action.");

            var valid = await db.Users.AnyAsync(
                x => x.Id == action.TargetUserId && x.IsActive && x.Role.Name == Domain.Common.RoleNames.SupportAgent, ct);
            if (!valid) return ToolResult.Fail($"User {action.TargetUserId} is not an active support agent.");
        }

        if (actionType == ApprovalActionType.ChangePriority && action.TargetPriority is null)
            return ToolResult.Fail("'targetPriority' is required for a ChangePriority action.");

        // Idempotence: do not stack duplicate pending requests for the same action on the same ticket.
        var duplicate = await db.AiApprovals.AnyAsync(
            a => a.WorkflowId == context.WorkflowId &&
                 a.ActionType == actionType &&
                 a.Status == ApprovalStatus.Pending, ct);
        if (duplicate)
            return ToolResult.Fail($"A pending {actionType} approval already exists for this workflow.");

        var risk = actionType == ApprovalActionType.Escalate ? RiskLevel.High : RiskLevel.Medium;
        var approval = new AiApproval
        {
            WorkflowId = context.WorkflowId,
            TicketId = context.TicketId,
            ActionType = actionType,
            ProposedActionJson = JsonSerializer.Serialize(action),
            Reason = reason,
            RiskLevel = risk,
            Status = ApprovalStatus.Pending,
            RequestedAt = clock.UtcNow
        };

        db.AiApprovals.Add(approval);
        await db.SaveChangesAsync(ct);

        return ToolResult.Ok(new
        {
            approvalId = approval.Id,
            status = approval.Status.ToString(),
            actionType = actionType.ToString(),
            message = "Approval requested. No change has been made to the ticket."
        });
    }
}

/// <summary>
/// Registered in the tool registry but present in NO agent's allow-list. It exists so the allow-list
/// mechanism is demonstrable: an agent that asks for it is refused and the refusal is audited.
/// Even if it were reached, it refuses unless a matching approval row is already Approved by a manager.
/// The real approval path calls IApprovalActionExecutor directly from the service layer.
/// </summary>
public sealed class ExecuteApprovedActionTool(IAppDbContext db, IApprovalActionExecutor executor) : IAgentTool
{
    public string Name => "ExecuteApprovedAction";
    public string Description => "Applies an action that a manager has already approved. Not available to any agent.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        if (!input.TryGetProperty("approvalId", out var a) || a.ValueKind != JsonValueKind.Number)
            return ToolResult.Fail("'approvalId' is required.");

        var approval = await db.AiApprovals
            .Include(x => x.DecidedByUser).ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(x => x.Id == a.GetInt32(), ct);

        if (approval is null) return ToolResult.Fail("Approval not found.");
        if (approval.WorkflowId != context.WorkflowId)
            return ToolResult.Fail("Approval does not belong to this workflow.");
        if (approval.Status != ApprovalStatus.Approved)
            return ToolResult.Fail("Action has not been approved by an authorized user.");

        // Defence in depth: re-check that the decider actually held an approving role.
        var deciderRole = approval.DecidedByUser?.Role.Name;
        if (deciderRole is not (Domain.Common.RoleNames.SupportManager or Domain.Common.RoleNames.Admin))
            return ToolResult.Fail("Approval was not decided by an authorized user.");

        await executor.ExecuteAsync(approval, ct);
        return ToolResult.Ok(new { approvalId = approval.Id, executed = true });
    }
}
