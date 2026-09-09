using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Services;

/// <summary>
/// The human-in-the-loop gate (spec section 9.8), enforced in the backend rather than in the UI.
///
/// Only a SupportManager or Admin can decide an approval, and that is checked here - a React build
/// that hid the buttons would change nothing. Rejecting or requesting revision executes nothing.
/// </summary>
public sealed class ApprovalService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IApprovalActionExecutor executor,
    IAuditService audit,
    IClock clock,
    ILogger<ApprovalService> logger)
{
    public async Task<PagedResult<ApprovalDto>> QueryAsync(ApprovalQuery query, CancellationToken ct = default)
    {
        var q = db.AiApprovals.AsNoTracking()
            .Include(a => a.Ticket)
            .Include(a => a.DecidedByUser)
            .AsQueryable();

        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status);

        q = query.Descending
            ? q.OrderByDescending(a => a.RequestedAt)
            : q.OrderBy(a => a.RequestedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        return new PagedResult<ApprovalDto>(
            items.Select(a => new ApprovalDto(
                a.Id, a.WorkflowId, a.TicketId, a.Ticket.TicketNumber, a.Ticket.Title,
                a.ActionType, a.ProposedActionJson, a.Reason, a.RiskLevel, a.Status,
                a.RequestedAt, a.DecidedByUser?.FullName, a.DecidedAt, a.DecisionNote)).ToList(),
            query.Page, query.PageSize, total);
    }

    /// <summary>
    /// Records a manager's decision and, only for Approved, executes the action.
    /// The whole method is the enforcement point: authorization, state check, execution, audit.
    /// </summary>
    public async Task<ApprovalDto> DecideAsync(int approvalId, ApprovalDecisionRequest request, CancellationToken ct = default)
    {
        // --- 1. Authorization. Checked in the service so every client is bound by it.
        if (!currentUser.IsInRole(RoleNames.SupportManager, RoleNames.Admin))
            throw new ForbiddenException("Only a support manager or administrator can decide an AI approval.");

        var userId = currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

        // --- 2. Only the three real decisions are accepted; "Pending" is not a decision.
        if (request.Decision is not (ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.RevisionRequested))
            throw new ValidationException("Decision must be Approved, Rejected or RevisionRequested.");

        var approval = await db.AiApprovals
            .Include(a => a.Ticket)
            .Include(a => a.Workflow)
            .FirstOrDefaultAsync(a => a.Id == approvalId, ct)
            ?? throw new NotFoundException("AiApproval", approvalId);

        // --- 3. An approval can only be decided once.
        if (approval.Status != ApprovalStatus.Pending)
            throw new ConflictException($"This approval has already been {approval.Status}.");

        var now = clock.UtcNow;
        approval.Status = request.Decision;
        approval.DecidedByUserId = userId;
        approval.DecidedAt = now;
        approval.DecisionNote = request.Note?.Trim();
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("AiApproval", approval.Id, $"Approval{request.Decision}",
            ActorType.User, userId,
            new { approval.ActionType, approval.TicketId, request.Note }, ct);

        // --- 4. Execute only on approval. Rejection and revision change nothing on the ticket.
        if (request.Decision == ApprovalStatus.Approved)
        {
            try
            {
                await executor.ExecuteAsync(approval, ct);
                await CompleteWorkflowIfSettledAsync(approval.WorkflowId, ct);
            }
            catch (Exception ex)
            {
                // A failure to execute must not leave the approval looking successful.
                logger.LogError(ex, "Executing approval {ApprovalId} failed", approval.Id);
                approval.Status = ApprovalStatus.Pending;
                approval.DecidedByUserId = null;
                approval.DecidedAt = null;
                approval.DecisionNote = $"Execution failed: {ex.Message}";
                await db.SaveChangesAsync(ct);

                await audit.LogAsync("AiApproval", approval.Id, "ApprovalExecutionFailed",
                    ActorType.System, userId, new { error = ex.Message }, ct);
                throw;
            }
        }
        else
        {
            await SettleWorkflowAsync(approval.WorkflowId, request.Decision, ct);
        }

        return await GetAsync(approval.Id, ct);
    }

    public async Task<ApprovalDto> GetAsync(int id, CancellationToken ct = default)
    {
        var a = await db.AiApprovals.AsNoTracking()
            .Include(x => x.Ticket).Include(x => x.DecidedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("AiApproval", id);

        return new ApprovalDto(
            a.Id, a.WorkflowId, a.TicketId, a.Ticket.TicketNumber, a.Ticket.Title,
            a.ActionType, a.ProposedActionJson, a.Reason, a.RiskLevel, a.Status,
            a.RequestedAt, a.DecidedByUser?.FullName, a.DecidedAt, a.DecisionNote);
    }

    /// <summary>A workflow is only Completed once none of its approvals is still Pending.</summary>
    private async Task CompleteWorkflowIfSettledAsync(int workflowId, CancellationToken ct)
    {
        var stillPending = await db.AiApprovals
            .AnyAsync(a => a.WorkflowId == workflowId && a.Status == ApprovalStatus.Pending, ct);
        if (stillPending) return;

        var workflow = await db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, ct);
        if (workflow is null) return;

        workflow.Status = WorkflowStatus.Completed;
        workflow.CurrentStep = "Completed";
        workflow.CompletedAt = clock.UtcNow;
        workflow.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("AgentWorkflow", workflowId, "WorkflowCompleted", ActorType.System,
            currentUser.UserId, new { reason = "All approvals decided" }, ct);
    }

    private async Task SettleWorkflowAsync(int workflowId, ApprovalStatus decision, CancellationToken ct)
    {
        var stillPending = await db.AiApprovals
            .AnyAsync(a => a.WorkflowId == workflowId && a.Status == ApprovalStatus.Pending, ct);
        if (stillPending) return;

        var workflow = await db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, ct);
        if (workflow is null) return;

        var anyApproved = await db.AiApprovals
            .AnyAsync(a => a.WorkflowId == workflowId && a.Status == ApprovalStatus.Approved, ct);

        workflow.Status = anyApproved ? WorkflowStatus.Completed : WorkflowStatus.Rejected;
        workflow.CurrentStep = workflow.Status.ToString();
        workflow.CompletedAt = clock.UtcNow;
        workflow.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("AgentWorkflow", workflowId,
            anyApproved ? "WorkflowCompleted" : "WorkflowRejected",
            ActorType.System, currentUser.UserId, new { decision = decision.ToString() }, ct);
    }
}
