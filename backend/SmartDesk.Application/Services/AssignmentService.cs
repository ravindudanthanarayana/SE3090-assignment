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
/// Component B - Support Agent and Assignment Management (Student 2).
/// Owns agent skills, workload visibility, the deterministic assignment recommendation and the
/// assignment business operation.
/// </summary>
public sealed class AssignmentService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditService audit,
    INotificationService notifications,
    IClock clock)
{
    public async Task<IReadOnlyList<SupportAgentDto>> GetSupportAgentsAsync(CancellationToken ct = default)
    {
        var open = TicketStatusMachine.OpenStatuses;
        var now = clock.UtcNow;

        var agents = await db.Users.AsNoTracking()
            .Where(u => u.Role.Name == RoleNames.SupportAgent)
            .Include(u => u.Skills).ThenInclude(s => s.Category)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        var ids = agents.Select(a => a.Id).ToList();

        // One query for all agents' tickets rather than a query per agent.
        var tickets = await db.Tickets.AsNoTracking()
            .Where(t => t.AssignedToUserId != null && ids.Contains(t.AssignedToUserId!.Value))
            .Select(t => new { t.AssignedToUserId, t.Status, t.SlaDueAt, t.CreatedAt, t.ResolvedAt })
            .ToListAsync(ct);

        var thirtyDaysAgo = now.AddDays(-30);

        return agents.Select(a =>
        {
            var mine = tickets.Where(t => t.AssignedToUserId == a.Id).ToList();
            var openTickets = mine.Where(t => open.Contains(t.Status)).ToList();

            var workload = new AgentWorkloadDto(
                a.Id, a.FullName,
                openTickets.Count,
                mine.Count(t => t.Status == TicketStatus.InProgress),
                openTickets.Count(t => t.SlaDueAt >= now &&
                    (t.SlaDueAt - now).TotalHours / Math.Max(1, (t.SlaDueAt - t.CreatedAt).TotalHours) <= SlaCalculator.AtRiskThreshold),
                openTickets.Count(t => t.SlaDueAt < now),
                mine.Count(t => t.ResolvedAt >= thirtyDaysAgo));

            return new SupportAgentDto(
                a.Id, a.FullName, a.Email, a.Department, a.IsActive,
                a.Skills.Select(s => new AgentSkillDto(s.Id, s.CategoryId, s.Category.Name, s.ProficiencyLevel))
                    .OrderBy(s => s.CategoryName).ToList(),
                workload);
        }).ToList();
    }

    public async Task<IReadOnlyList<AgentWorkloadDto>> GetWorkloadAsync(CancellationToken ct = default)
        => (await GetSupportAgentsAsync(ct)).Select(a => a.Workload).ToList();

    public async Task<IReadOnlyList<AgentSkillDto>> GetSkillsAsync(int userId, CancellationToken ct = default)
    {
        // A support agent may read their own skills; managers and admins may read anyone's.
        if (!currentUser.IsInRole(RoleNames.SupportManager, RoleNames.Admin) && currentUser.UserId != userId)
            throw new ForbiddenException("You may only view your own skills.");

        return await db.SupportAgentSkills.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Category.Name)
            .Select(s => new AgentSkillDto(s.Id, s.CategoryId, s.Category.Name, s.ProficiencyLevel))
            .ToListAsync(ct);
    }

    public async Task<AgentSkillDto> UpsertSkillAsync(int userId, UpsertSkillRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (user.Role.Name != RoleNames.SupportAgent)
            throw new ValidationException("Skills can only be assigned to support agents.");

        var category = await db.TicketCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new ValidationException($"Category {request.CategoryId} does not exist.");

        var now = clock.UtcNow;
        var skill = await db.SupportAgentSkills
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CategoryId == request.CategoryId, ct);

        if (skill is null)
        {
            skill = new SupportAgentSkill
            {
                UserId = userId, CategoryId = request.CategoryId,
                ProficiencyLevel = request.ProficiencyLevel, CreatedAt = now, UpdatedAt = now
            };
            db.SupportAgentSkills.Add(skill);
        }
        else
        {
            skill.ProficiencyLevel = request.ProficiencyLevel;
            skill.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("SupportAgentSkill", skill.Id, "SkillUpserted", ActorType.User, currentUser.UserId,
            new { userId, request.CategoryId, request.ProficiencyLevel }, ct);

        return new AgentSkillDto(skill.Id, category.Id, category.Name, skill.ProficiencyLevel);
    }

    public async Task DeleteSkillAsync(int userId, int skillId, CancellationToken ct = default)
    {
        var skill = await db.SupportAgentSkills.FirstOrDefaultAsync(s => s.Id == skillId && s.UserId == userId, ct)
            ?? throw new NotFoundException("Skill", skillId);

        db.SupportAgentSkills.Remove(skill);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("SupportAgentSkill", skillId, "SkillDeleted", ActorType.User, currentUser.UserId, null, ct);
    }

    /// <summary>
    /// Business operation beyond CRUD: the deterministic skill-versus-workload ranking.
    /// The Assignment agent's ScoreAssignmentCandidates tool runs the same AssignmentScorer,
    /// so a manager and the AI can never see different numbers.
    /// </summary>
    public async Task<AssignmentRecommendationDto> RecommendAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        var open = TicketStatusMachine.OpenStatuses;
        var candidates = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role.Name == RoleNames.SupportAgent)
            .Select(u => new CandidateInput(
                u.Id,
                u.FullName,
                u.Skills.Where(s => s.CategoryId == ticket.CategoryId).Select(s => s.ProficiencyLevel).FirstOrDefault(),
                db.Tickets.Count(t => t.AssignedToUserId == u.Id && open.Contains(t.Status))))
            .ToListAsync(ct);

        var ranked = AssignmentScorer.Rank(candidates);

        return new AssignmentRecommendationDto(
            ticketId,
            ranked.Select(r => new AssignmentCandidateDto(
                r.UserId, r.FullName, r.Score, r.SkillLevel, r.OpenTicketCount, r.Explanation)).ToList(),
            ranked.Count > 0 ? ranked[0].UserId : null);
    }

    /// <summary>
    /// Business operation beyond CRUD: assign or reassign a ticket. Managers and admins only.
    /// Records the assignment history and moves a New ticket to Assigned.
    /// </summary>
    public async Task<TicketAssignmentDto> AssignAsync(
        int ticketId, AssignTicketRequest request, AssignmentSource source = AssignmentSource.Manual,
        CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        if (ticket.Status is TicketStatus.Closed or TicketStatus.Cancelled)
            throw new ConflictException($"A {ticket.Status} ticket cannot be assigned.");

        var assignee = await db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == request.AssignedToUserId, ct)
            ?? throw new ValidationException($"User {request.AssignedToUserId} does not exist.");

        if (!assignee.IsActive || assignee.Role.Name != RoleNames.SupportAgent)
            throw new ValidationException("Tickets can only be assigned to an active support agent.");

        var now = clock.UtcNow;
        var previous = ticket.AssignedToUserId;
        var actorId = currentUser.UserId;

        ticket.AssignedToUserId = assignee.Id;
        if (ticket.Status == TicketStatus.New) ticket.Status = TicketStatus.Assigned;
        ticket.UpdatedAt = now;

        var assignment = new TicketAssignment
        {
            TicketId = ticketId,
            AssignedToUserId = assignee.Id,
            AssignedByUserId = actorId,
            Reason = request.Reason,
            Source = source,
            CreatedAt = now
        };
        db.TicketAssignments.Add(assignment);

        db.TicketHistory.Add(new TicketHistoryEntry
        {
            TicketId = ticketId,
            // A null actor marks an approved AI action, which keeps the trail honest about who acted.
            ChangedByUserId = source == AssignmentSource.AiApproved ? null : actorId,
            Field = "AssignedTo",
            OldValue = previous?.ToString(),
            NewValue = assignee.Id.ToString(),
            Note = source == AssignmentSource.AiApproved
                ? $"Assigned via approved AI recommendation. {request.Reason}"
                : request.Reason,
            CreatedAt = now
        });

        await db.SaveChangesAsync(ct);

        await audit.LogAsync("Ticket", ticketId,
            previous is null ? "TicketAssigned" : "TicketReassigned",
            source == AssignmentSource.AiApproved ? ActorType.Agent : ActorType.User, actorId,
            new { from = previous, to = assignee.Id, source = source.ToString(), request.Reason }, ct);

        // Third-party notification: tell the agent they now own this work.
        await notifications.NotifyAsync(assignee.Id, ticketId,
            $"Ticket {ticket.TicketNumber} assigned to you",
            $"You have been assigned \"{ticket.Title}\" (priority {ticket.Priority}, due {ticket.SlaDueAt:u}).", ct);

        var assignedBy = actorId is null ? null : await db.Users.AsNoTracking()
            .Where(u => u.Id == actorId).Select(u => u.FullName).FirstOrDefaultAsync(ct);

        return new TicketAssignmentDto(assignment.Id, assignee.Id, assignee.FullName, assignedBy,
            assignment.Reason, assignment.Source, assignment.CreatedAt);
    }

    public async Task<IReadOnlyList<TicketAssignmentDto>> GetAssignmentHistoryAsync(int ticketId, CancellationToken ct = default)
        => await db.TicketAssignments.AsNoTracking()
            .Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new TicketAssignmentDto(
                a.Id, a.AssignedToUserId, a.AssignedToUser.FullName,
                a.AssignedByUser != null ? a.AssignedByUser.FullName : "System / AI",
                a.Reason, a.Source, a.CreatedAt))
            .ToListAsync(ct);
}
