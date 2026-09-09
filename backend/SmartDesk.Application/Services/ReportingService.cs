using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.BusinessRules;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Services;

/// <summary>
/// Component D - SLA, Escalation and Reporting (Student 4).
/// Owns the dashboard, SLA and performance reports, and the escalation business operation.
/// </summary>
public sealed class ReportingService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditService audit,
    INotificationService notifications,
    IClock clock)
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var open = TicketStatusMachine.OpenStatuses;

        var q = db.Tickets.AsNoTracking().Include(t => t.Category).AsQueryable();

        // An employee's dashboard shows only their own tickets.
        if (currentUser.IsInRole(RoleNames.Employee))
        {
            var me = currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");
            q = q.Where(t => t.CreatedByUserId == me);
        }

        var tickets = await q
            .Select(t => new { t.Status, t.Priority, t.SlaDueAt, t.CreatedAt, t.IsEscalated, CategoryName = t.Category.Name })
            .ToListAsync(ct);

        int AtRisk() => tickets.Count(t =>
            open.Contains(t.Status) && t.SlaDueAt >= now &&
            (t.SlaDueAt - now).TotalHours / Math.Max(1, (t.SlaDueAt - t.CreatedAt).TotalHours) <= SlaCalculator.AtRiskThreshold);

        var activeWorkflows = await db.AgentWorkflows
            .CountAsync(w => w.Status == WorkflowStatus.Running || w.Status == WorkflowStatus.AwaitingApproval, ct);
        var pendingApprovals = await db.AiApprovals.CountAsync(a => a.Status == ApprovalStatus.Pending, ct);

        return new DashboardDto(
            TotalTickets: tickets.Count,
            OpenTickets: tickets.Count(t => open.Contains(t.Status)),
            InProgressTickets: tickets.Count(t => t.Status == TicketStatus.InProgress),
            ResolvedTickets: tickets.Count(t => t.Status == TicketStatus.Resolved),
            ClosedTickets: tickets.Count(t => t.Status == TicketStatus.Closed),
            HighPriorityTickets: tickets.Count(t => t.Priority >= TicketPriority.High && open.Contains(t.Status)),
            EscalatedTickets: tickets.Count(t => t.IsEscalated),
            SlaAtRiskTickets: AtRisk(),
            SlaBreachedTickets: tickets.Count(t => open.Contains(t.Status) && t.SlaDueAt < now),
            ActiveWorkflows: activeWorkflows,
            PendingApprovals: pendingApprovals,
            ByStatus: tickets.GroupBy(t => t.Status)
                .Select(g => new CountByLabelDto(g.Key.ToString(), g.Count()))
                .OrderBy(x => x.Label).ToList(),
            ByPriority: tickets.GroupBy(t => t.Priority)
                .Select(g => new CountByLabelDto(g.Key.ToString(), g.Count()))
                .OrderBy(x => x.Label).ToList(),
            ByCategory: tickets.GroupBy(t => t.CategoryName)
                .Select(g => new CountByLabelDto(g.Key, g.Count()))
                .OrderByDescending(x => x.Count).ToList());
    }

    public async Task<SlaReportDto> GetSlaReportAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var tickets = await db.Tickets.AsNoTracking().Include(t => t.Category).ToListAsync(ct);

        var live = tickets.Where(t => TicketStatusMachine.IsOpen(t.Status)).ToList();
        var states = live.ToDictionary(t => t.Id, t => SlaCalculator.GetState(t, now));

        var breached = states.Count(s => s.Value == SlaState.Breached);
        var atRisk = states.Count(s => s.Value == SlaState.AtRisk);
        var onTrack = states.Count(s => s.Value == SlaState.OnTrack);

        var byCategory = live
            .GroupBy(t => t.Category.Name)
            .Select(g => new SlaCategoryBreakdownDto(
                g.Key, g.Count(),
                g.Count(t => states[t.Id] == SlaState.AtRisk),
                g.Count(t => states[t.Id] == SlaState.Breached)))
            .OrderByDescending(x => x.Breached)
            .ToList();

        var rate = live.Count == 0 ? 0 : Math.Round(breached * 100.0 / live.Count, 2);
        return new SlaReportDto(onTrack, atRisk, breached, rate, byCategory);
    }

    public async Task<IReadOnlyList<SlaAtRiskTicketDto>> GetAtRiskAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var open = TicketStatusMachine.OpenStatuses;

        var q = db.Tickets.AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Where(t => open.Contains(t.Status));

        // A support agent sees only the tickets they are responsible for.
        if (currentUser.IsInRole(RoleNames.SupportAgent))
            q = q.Where(t => t.AssignedToUserId == currentUser.UserId);

        var tickets = await q.ToListAsync(ct);

        return tickets
            .Select(t => new { Ticket = t, State = SlaCalculator.GetState(t, now) })
            .Where(x => x.State is SlaState.AtRisk or SlaState.Breached)
            .OrderBy(x => x.Ticket.SlaDueAt)
            .Select(x => new SlaAtRiskTicketDto(
                x.Ticket.Id, x.Ticket.TicketNumber, x.Ticket.Title, x.Ticket.Priority, x.Ticket.Status,
                x.Ticket.AssignedToUser?.FullName, x.Ticket.SlaDueAt,
                SlaCalculator.HoursRemaining(x.Ticket, now), x.State))
            .ToList();
    }

    public async Task<IReadOnlyList<AgentPerformanceDto>> GetAgentPerformanceAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var open = TicketStatusMachine.OpenStatuses;

        var agents = await db.Users.AsNoTracking()
            .Where(u => u.Role.Name == RoleNames.SupportAgent)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        var ids = agents.Select(a => a.Id).ToList();
        var tickets = await db.Tickets.AsNoTracking()
            .Where(t => t.AssignedToUserId != null && ids.Contains(t.AssignedToUserId!.Value))
            .Select(t => new { t.AssignedToUserId, t.Status, t.CreatedAt, t.ResolvedAt, t.SlaDueAt })
            .ToListAsync(ct);

        return agents.Select(a =>
        {
            var mine = tickets.Where(t => t.AssignedToUserId == a.Id).ToList();
            var resolved = mine.Where(t => t.ResolvedAt.HasValue).ToList();
            var breached = mine.Count(t => t.ResolvedAt.HasValue
                ? t.ResolvedAt > t.SlaDueAt
                : open.Contains(t.Status) && t.SlaDueAt < now);

            var avgHours = resolved.Count == 0
                ? 0
                : Math.Round(resolved.Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours), 2);

            return new AgentPerformanceDto(
                a.Id, a.FullName, mine.Count, resolved.Count,
                mine.Count(t => open.Contains(t.Status)), avgHours, breached,
                mine.Count == 0 ? 0 : Math.Round(breached * 100.0 / mine.Count, 2));
        })
        .OrderByDescending(a => a.Resolved)
        .ToList();
    }

    /// <summary>
    /// Business operation beyond CRUD: manual escalation by a manager.
    /// The AI never reaches this method - an AI-proposed escalation must first be approved,
    /// and ApprovalActionExecutor performs the equivalent write inside a transaction.
    /// </summary>
    public async Task EscalateAsync(int ticketId, EscalateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        if (ticket.IsEscalated)
            throw new ConflictException("This ticket has already been escalated.");

        if (!TicketStatusMachine.CanTransition(ticket.Status, TicketStatus.Escalated))
            throw new ConflictException($"A {ticket.Status} ticket cannot be escalated.");

        var now = clock.UtcNow;
        var old = ticket.Status;
        var actorId = currentUser.UserId;

        ticket.Status = TicketStatus.Escalated;
        ticket.IsEscalated = true;
        ticket.EscalatedAt = now;
        ticket.EscalationReason = request.Reason.Trim();
        ticket.UpdatedAt = now;

        db.TicketHistory.Add(new TicketHistoryEntry
        {
            TicketId = ticketId, ChangedByUserId = actorId,
            Field = "Status", OldValue = old.ToString(), NewValue = TicketStatus.Escalated.ToString(),
            Note = $"Manually escalated: {request.Reason.Trim()}", CreatedAt = now
        });

        await db.SaveChangesAsync(ct);

        await audit.LogAsync("Ticket", ticketId, "TicketEscalated", ActorType.User, actorId,
            new { reason = request.Reason, from = old.ToString() }, ct);

        await notifications.NotifyAsync(ticket.CreatedByUserId, ticketId,
            $"Ticket {ticket.TicketNumber} has been escalated",
            $"Your ticket \"{ticket.Title}\" was escalated. Reason: {request.Reason.Trim()}", ct);
    }
}
