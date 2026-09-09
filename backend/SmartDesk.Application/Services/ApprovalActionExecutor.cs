using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.BusinessRules;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Services;

/// <summary>
/// Applies an approved high-impact action. This is the only code path that turns an AI recommendation
/// into a real change, and it runs inside a single transaction so the ticket, its history, the
/// assignment record, the approval row and the audit entry either all change or none do
/// (spec section 6.4).
/// </summary>
public sealed class ApprovalActionExecutor(
    IAppDbContext db,
    IAuditService audit,
    INotificationService notifications,
    IClock clock) : IApprovalActionExecutor
{
    public async Task ExecuteAsync(AiApproval approval, CancellationToken ct = default)
    {
        // Defence in depth: this is re-checked here even though ApprovalService has already checked it.
        if (approval.Status != ApprovalStatus.Approved)
            throw new ForbiddenException("This action has not been approved.");

        var action = JsonSerializer.Deserialize<ProposedAction>(approval.ProposedActionJson)
            ?? throw new ValidationException("The approved action payload is unreadable.");

        // The ticket always comes from the approval row, never from the deserialized payload,
        // so a tampered payload cannot redirect the action at a different ticket.
        var ticket = await db.Tickets.Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == approval.TicketId, ct)
            ?? throw new NotFoundException("Ticket", approval.TicketId);

        var now = clock.UtcNow;

        // The connection uses EnableRetryOnFailure, because Neon is serverless and can drop an idle
        // connection. A retrying strategy refuses a hand-rolled transaction unless the whole
        // transaction is handed to it as one retriable unit, which is what this does. Every write
        // below sets a value rather than incrementing one, so replaying the unit is safe.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.BeginTransactionAsync(ct);
            try
            {
                switch (approval.ActionType)
                {
                    case ApprovalActionType.Escalate:
                        await EscalateAsync(ticket, approval, now, ct);
                        break;

                    case ApprovalActionType.Assign:
                        await AssignAsync(ticket, approval, action, now, ct);
                        break;

                    case ApprovalActionType.ChangePriority:
                        await ChangePriorityAsync(ticket, approval, action, now, ct);
                        break;

                    default:
                        throw new ValidationException($"Unsupported action type {approval.ActionType}.");
                }

                ticket.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        await audit.LogAsync("Ticket", ticket.Id, $"Ai{approval.ActionType}Executed",
            ActorType.Agent, approval.DecidedByUserId,
            new { approvalId = approval.Id, approval.ActionType, approvedBy = approval.DecidedByUserId }, ct);
    }

    private async Task EscalateAsync(Ticket ticket, AiApproval approval, DateTime now, CancellationToken ct)
    {
        var old = ticket.Status;

        ticket.Status = TicketStatus.Escalated;
        ticket.IsEscalated = true;
        ticket.EscalatedAt = now;
        ticket.EscalationReason = approval.Reason;

        AddHistory(ticket.Id, "Status", old.ToString(), TicketStatus.Escalated.ToString(),
            $"Escalated via approved AI recommendation (approval #{approval.Id}). {approval.Reason}", now);

        await notifications.NotifyAsync(ticket.CreatedByUserId, ticket.Id,
            $"Ticket {ticket.TicketNumber} has been escalated",
            $"Your ticket \"{ticket.Title}\" was escalated after a manager approved an AI recommendation. " +
            $"Reason: {approval.Reason}", ct);
    }

    private async Task AssignAsync(Ticket ticket, AiApproval approval, ProposedAction action, DateTime now, CancellationToken ct)
    {
        if (action.TargetUserId is not int targetUserId)
            throw new ValidationException("The approved assignment does not name a user.");

        // Re-validate at execution time: the person may have been deactivated or had their role changed
        // between the recommendation and the approval.
        var assignee = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new ValidationException($"User {targetUserId} no longer exists.");

        if (!assignee.IsActive || assignee.Role.Name != Domain.Common.RoleNames.SupportAgent)
            throw new ValidationException($"{assignee.FullName} is no longer an active support agent.");

        var previous = ticket.AssignedToUserId;
        ticket.AssignedToUserId = assignee.Id;
        if (ticket.Status == TicketStatus.New) ticket.Status = TicketStatus.Assigned;

        db.TicketAssignments.Add(new TicketAssignment
        {
            TicketId = ticket.Id,
            AssignedToUserId = assignee.Id,
            AssignedByUserId = approval.DecidedByUserId,
            Reason = $"Approved AI recommendation: {approval.Reason}",
            Source = AssignmentSource.AiApproved,
            CreatedAt = now
        });

        AddHistory(ticket.Id, "AssignedTo", previous?.ToString(), assignee.Id.ToString(),
            $"Assigned via approved AI recommendation (approval #{approval.Id}).", now);

        await notifications.NotifyAsync(assignee.Id, ticket.Id,
            $"Ticket {ticket.TicketNumber} assigned to you",
            $"A manager approved an AI recommendation to assign you \"{ticket.Title}\".", ct);
    }

    private Task ChangePriorityAsync(Ticket ticket, AiApproval approval, ProposedAction action, DateTime now, CancellationToken ct)
    {
        if (action.TargetPriority is not TicketPriority target)
            throw new ValidationException("The approved priority change does not name a priority.");

        var old = ticket.Priority;
        ticket.Priority = target;
        // The SLA deadline is recomputed here in C#, never taken from the agent's payload.
        ticket.SlaDueAt = SlaCalculator.CalculateDueAt(ticket.CreatedAt, ticket.Category.DefaultSlaHours, target);

        AddHistory(ticket.Id, "Priority", old.ToString(), target.ToString(),
            $"Changed via approved AI recommendation (approval #{approval.Id}). {approval.Reason}", now);

        return Task.CompletedTask;
    }

    private void AddHistory(int ticketId, string field, string? oldValue, string? newValue, string note, DateTime now)
    {
        db.TicketHistory.Add(new TicketHistoryEntry
        {
            TicketId = ticketId,
            ChangedByUserId = null, // null marks a system/agent action, distinct from a person's edit
            Field = field, OldValue = oldValue, NewValue = newValue, Note = note, CreatedAt = now
        });
    }
}
