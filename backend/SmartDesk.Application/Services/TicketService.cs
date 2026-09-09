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
/// Component A - Ticket Management (Student 1).
/// Owns ticket CRUD, the search/filter/sort/pagination query, the status transition business
/// operation, comments and history.
/// </summary>
public sealed class TicketService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditService audit,
    INotificationService notifications,
    IClock clock)
{
    // ---- Read ---------------------------------------------------------------------------

    /// <summary>
    /// Server-side search, filtering, sorting and pagination (spec section 4.4).
    /// Employees are silently scoped to their own tickets; support agents default to their queue.
    /// </summary>
    public async Task<PagedResult<TicketListItemDto>> QueryAsync(TicketQuery query, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var open = TicketStatusMachine.OpenStatuses;
        var q = BuildFilteredQuery(query, now, open);

        // --- Sorting. Column names are allow-listed; anything unknown falls back to CreatedAt.
        q = (query.SortBy?.ToLowerInvariant(), query.Descending) switch
        {
            ("priority", true) => q.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt),
            ("priority", false) => q.OrderBy(t => t.Priority).ThenByDescending(t => t.CreatedAt),
            ("sladueat", true) => q.OrderByDescending(t => t.SlaDueAt),
            ("sladueat", false) => q.OrderBy(t => t.SlaDueAt),
            ("status", true) => q.OrderByDescending(t => t.Status).ThenByDescending(t => t.CreatedAt),
            ("status", false) => q.OrderBy(t => t.Status).ThenByDescending(t => t.CreatedAt),
            ("title", true) => q.OrderByDescending(t => t.Title),
            ("title", false) => q.OrderBy(t => t.Title),
            ("createdat", false) => q.OrderBy(t => t.CreatedAt),
            _ => q.OrderByDescending(t => t.CreatedAt)
        };

        // "At risk" and "on track" both depend on the elapsed fraction of the SLA window, which SQL
        // cannot express as cheaply as it expresses a breach. BuildFilteredQuery has already narrowed
        // this to still-open, not-yet-breached tickets, so the refinement runs over a small set.
        if (query.SlaState is Domain.Enums.SlaState.AtRisk or Domain.Enums.SlaState.OnTrack)
        {
            var candidates = await q.ToListAsync(ct);
            var matching = candidates
                .Where(t => SlaCalculator.GetState(t, now) == query.SlaState)
                .ToList();

            var page = matching
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(t => ToListItem(t, now))
                .ToList();

            return new PagedResult<TicketListItemDto>(page, query.Page, query.PageSize, matching.Count);
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TicketListItemDto>(
            items.Select(t => ToListItem(t, now)).ToList(), query.Page, query.PageSize, total);
    }

    /// <summary>
    /// Applies authorization scoping, search and every filter. Kept separate so the sort and paging
    /// logic above reads clearly, and so the at-risk path reuses exactly the same predicates.
    /// </summary>
    private IQueryable<Ticket> BuildFilteredQuery(TicketQuery query, DateTime now, TicketStatus[] open)
    {
        var q = db.Tickets.AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .AsQueryable();

        // Authorization is part of the query, so an employee's own scope is enforced in SQL
        // rather than by filtering rows after they have already been read.
        if (currentUser.IsInRole(RoleNames.Employee))
        {
            var me = RequireUserId();
            q = q.Where(t => t.CreatedByUserId == me);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower + Contains translates to lower(...) LIKE '%...%' on PostgreSQL and also works
            // under the in-memory provider used by the unit tests.
            var term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(t =>
                t.TicketNumber.ToLower().Contains(term) ||
                t.Title.ToLower().Contains(term) ||
                t.Description.ToLower().Contains(term));
        }

        if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status);
        if (query.Priority.HasValue) q = q.Where(t => t.Priority == query.Priority);
        if (query.CategoryId.HasValue) q = q.Where(t => t.CategoryId == query.CategoryId);
        if (query.AssignedToUserId.HasValue) q = q.Where(t => t.AssignedToUserId == query.AssignedToUserId);
        if (query.Unassigned == true) q = q.Where(t => t.AssignedToUserId == null);

        q = query.SlaState switch
        {
            Domain.Enums.SlaState.Breached => q.Where(t => open.Contains(t.Status) && t.SlaDueAt < now),
            Domain.Enums.SlaState.AtRisk => q.Where(t => open.Contains(t.Status) && t.SlaDueAt >= now),
            Domain.Enums.SlaState.OnTrack => q.Where(t => open.Contains(t.Status) && t.SlaDueAt >= now),
            Domain.Enums.SlaState.NotApplicable => q.Where(t => !open.Contains(t.Status)),
            _ => q
        };

        return q;
    }

    public async Task<TicketDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.ArticleLinks).ThenInclude(l => l.Article)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("Ticket", id);

        EnsureCanView(ticket);

        var now = clock.UtcNow;
        return new TicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.Title, ticket.Description,
            ticket.CategoryId, ticket.Category.Name, ticket.Status, ticket.Priority,
            ticket.CreatedByUserId, ticket.CreatedByUser.FullName,
            ticket.AssignedToUserId, ticket.AssignedToUser?.FullName,
            ticket.SlaDueAt, SlaCalculator.GetState(ticket, now), SlaCalculator.HoursRemaining(ticket, now),
            ticket.IsEscalated, ticket.EscalatedAt, ticket.EscalationReason,
            ticket.Resolution, ticket.ResolvedAt, ticket.ClosedAt,
            ticket.CreatedAt, ticket.UpdatedAt,
            TicketStatusMachine.AllowedNext(ticket.Status),
            ticket.ArticleLinks
                .OrderByDescending(l => l.RelevanceScore)
                .Select(l => new LinkedArticleDto(l.ArticleId, l.Article.Title, l.RelevanceScore, l.Source))
                .ToList());
    }

    // ---- Write --------------------------------------------------------------------------

    public async Task<TicketDetailDto> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();

        var category = await db.TicketCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.IsActive, ct)
            ?? throw new ValidationException($"Category {request.CategoryId} does not exist or is inactive.");

        var now = clock.UtcNow;
        var ticket = new Ticket
        {
            TicketNumber = await NextTicketNumberAsync(ct),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = category.Id,
            Status = TicketStatus.New,
            Priority = request.Priority,
            CreatedByUserId = userId,
            // The SLA deadline is always computed here in C#, never supplied by the client or an agent.
            SlaDueAt = SlaCalculator.CalculateDueAt(now, category.DefaultSlaHours, request.Priority),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        db.TicketHistory.Add(new TicketHistoryEntry
        {
            TicketId = ticket.Id, ChangedByUserId = userId,
            Field = "Status", OldValue = null, NewValue = TicketStatus.New.ToString(),
            Note = "Ticket created", CreatedAt = now
        });
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("Ticket", ticket.Id, "TicketCreated", ActorType.User, userId,
            new { ticket.TicketNumber, ticket.Title, ticket.Priority }, ct);

        return await GetAsync(ticket.Id, ct);
    }

    public async Task<TicketDetailDto> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("Ticket", id);

        EnsureCanEdit(ticket);

        var category = await db.TicketCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.IsActive, ct)
            ?? throw new ValidationException($"Category {request.CategoryId} does not exist or is inactive.");

        var userId = RequireUserId();
        var now = clock.UtcNow;

        if (ticket.Title != request.Title.Trim())
            AddHistory(ticket.Id, userId, "Title", ticket.Title, request.Title.Trim(), now);
        if (ticket.Priority != request.Priority)
        {
            AddHistory(ticket.Id, userId, "Priority", ticket.Priority.ToString(), request.Priority.ToString(), now);
            ticket.SlaDueAt = SlaCalculator.CalculateDueAt(ticket.CreatedAt, category.DefaultSlaHours, request.Priority);
        }
        if (ticket.CategoryId != category.Id)
            AddHistory(ticket.Id, userId, "Category", ticket.Category.Name, category.Name, now);

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.CategoryId = category.Id;
        ticket.Priority = request.Priority;
        ticket.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("Ticket", ticket.Id, "TicketUpdated", ActorType.User, userId, null, ct);

        return await GetAsync(ticket.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("Ticket", id);

        // Deleting work that has already been acted on would destroy the audit trail, so it is refused.
        if (ticket.Status is not (TicketStatus.New or TicketStatus.Cancelled))
            throw new ConflictException($"Only New or Cancelled tickets can be deleted; this ticket is {ticket.Status}.");

        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("Ticket", id, "TicketDeleted", ActorType.User, currentUser.UserId,
            new { ticket.TicketNumber }, ct);
    }

    /// <summary>
    /// Business operation beyond CRUD (spec section 5.7): a validated status transition that
    /// writes history and notifies the requester.
    /// </summary>
    public async Task<TicketDetailDto> ChangeStatusAsync(int id, ChangeStatusRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("Ticket", id);

        EnsureCanWorkOn(ticket);

        if (!TicketStatusMachine.CanTransition(ticket.Status, request.Status))
            throw new ConflictException(
                $"Cannot move a ticket from {ticket.Status} to {request.Status}. " +
                $"Allowed: {string.Join(", ", TicketStatusMachine.AllowedNext(ticket.Status))}.");

        if (request.Status == TicketStatus.Resolved && string.IsNullOrWhiteSpace(request.Resolution))
            throw new ValidationException("A resolution is required when resolving a ticket.");

        // Escalation carries side effects, so it goes through the dedicated escalation path only.
        if (request.Status == TicketStatus.Escalated)
            throw new ConflictException("Use POST /api/tickets/{id}/escalate to escalate a ticket.");

        var userId = RequireUserId();
        var now = clock.UtcNow;
        var old = ticket.Status;

        ticket.Status = request.Status;
        ticket.UpdatedAt = now;

        if (request.Status == TicketStatus.Resolved)
        {
            ticket.Resolution = request.Resolution!.Trim();
            ticket.ResolvedAt = now;
        }
        if (request.Status == TicketStatus.Closed) ticket.ClosedAt = now;

        AddHistory(ticket.Id, userId, "Status", old.ToString(), request.Status.ToString(), now, request.Note);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("Ticket", ticket.Id, "TicketStatusChanged", ActorType.User, userId,
            new { from = old.ToString(), to = request.Status.ToString(), request.Note }, ct);

        await notifications.NotifyAsync(ticket.CreatedByUserId, ticket.Id,
            $"Ticket {ticket.TicketNumber} is now {request.Status}",
            $"Your ticket \"{ticket.Title}\" moved from {old} to {request.Status}.", ct);

        return await GetAsync(ticket.Id, ct);
    }

    // ---- Comments and history -------------------------------------------------------------

    public async Task<IReadOnlyList<TicketCommentDto>> GetCommentsAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);
        EnsureCanView(ticket);

        var isEmployee = currentUser.IsInRole(RoleNames.Employee);

        return await db.TicketComments.AsNoTracking()
            .Where(c => c.TicketId == ticketId)
            // Internal staff notes are filtered out for the requester, in SQL.
            .Where(c => !isEmployee || !c.IsInternal)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TicketCommentDto(c.Id, c.AuthorUserId, c.AuthorUser.FullName, c.Body, c.IsInternal, c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<TicketCommentDto> AddCommentAsync(int ticketId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);
        EnsureCanView(ticket);

        var userId = RequireUserId();

        // Only staff may write internal notes; an employee asking for one silently gets a public comment.
        var isInternal = request.IsInternal && !currentUser.IsInRole(RoleNames.Employee);

        var comment = new TicketComment
        {
            TicketId = ticketId,
            AuthorUserId = userId,
            Body = request.Body.Trim(),
            IsInternal = isInternal,
            CreatedAt = clock.UtcNow
        };
        db.TicketComments.Add(comment);
        ticket.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("Ticket", ticketId, "CommentAdded", ActorType.User, userId, new { isInternal }, ct);

        var author = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        return new TicketCommentDto(comment.Id, userId, author.FullName, comment.Body, comment.IsInternal, comment.CreatedAt);
    }

    public async Task<IReadOnlyList<TicketHistoryDto>> GetHistoryAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);
        EnsureCanView(ticket);

        return await db.TicketHistory.AsNoTracking()
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new TicketHistoryDto(
                h.Id,
                // A null actor means the change came from the system or an approved agent action.
                h.ChangedByUser != null ? h.ChangedByUser.FullName : "System / AI",
                h.Field, h.OldValue, h.NewValue, h.Note, h.CreatedAt))
            .ToListAsync(ct);
    }

    // Images are intentionally persisted with the ticket rather than placed in a public file folder:
    // the same ownership check protects both the metadata and the bytes.
    public async Task<TicketAttachmentDto> AddAttachmentAsync(int ticketId, string fileName, string contentType,
        Stream content, long sizeBytes, CancellationToken ct = default)
    {
        const long maxSize = 5 * 1024 * 1024;
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);
        EnsureCanView(ticket);

        if (sizeBytes <= 0 || sizeBytes > maxSize)
            throw new ValidationException("Attachment must be an image no larger than 5 MB.");
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Only image attachments are allowed.");

        await using var memory = new MemoryStream();
        await content.CopyToAsync(memory, ct);
        if (memory.Length != sizeBytes || memory.Length > maxSize)
            throw new ValidationException("The uploaded attachment is invalid or too large.");

        var userId = RequireUserId();
        var attachment = new TicketAttachment
        {
            TicketId = ticketId,
            UploadedByUserId = userId,
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            SizeBytes = memory.Length,
            Content = memory.ToArray(),
            CreatedAt = clock.UtcNow
        };
        db.TicketAttachments.Add(attachment);
        ticket.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("Ticket", ticketId, "AttachmentAdded", ActorType.User, userId,
            new { attachment.FileName, attachment.ContentType, attachment.SizeBytes }, ct);
        return ToAttachmentDto(attachment);
    }

    public async Task<IReadOnlyList<TicketAttachmentDto>> GetAttachmentsAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);
        EnsureCanView(ticket);
        return await db.TicketAttachments.AsNoTracking().Where(a => a.TicketId == ticketId)
            .OrderBy(a => a.CreatedAt).Select(a => new TicketAttachmentDto(a.Id, a.FileName, a.ContentType,
                a.SizeBytes, $"/api/tickets/{ticketId}/attachments/{a.Id}/content", a.CreatedAt)).ToListAsync(ct);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> GetAttachmentContentAsync(
        int ticketId, int attachmentId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);
        EnsureCanView(ticket);
        var attachment = await db.TicketAttachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId, ct)
            ?? throw new NotFoundException("TicketAttachment", attachmentId);
        return (attachment.Content, attachment.ContentType, attachment.FileName);
    }

    private static TicketAttachmentDto ToAttachmentDto(TicketAttachment attachment) => new(attachment.Id,
        attachment.FileName, attachment.ContentType, attachment.SizeBytes,
        $"/api/tickets/{attachment.TicketId}/attachments/{attachment.Id}/content", attachment.CreatedAt);

    // ---- Helpers ---------------------------------------------------------------------------

    private int RequireUserId() =>
        currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

    /// <summary>An employee may only ever see their own tickets. Staff see all.</summary>
    private void EnsureCanView(Ticket ticket)
    {
        if (currentUser.IsInRole(RoleNames.Employee) && ticket.CreatedByUserId != currentUser.UserId)
            throw new ForbiddenException("You may only view your own tickets.");
    }

    /// <summary>Editing the ticket text: the requester while it is still New, or any staff member.</summary>
    private void EnsureCanEdit(Ticket ticket)
    {
        if (currentUser.IsInRole(RoleNames.SupportManager, RoleNames.Admin)) return;

        if (currentUser.IsInRole(RoleNames.SupportAgent))
        {
            if (ticket.AssignedToUserId != currentUser.UserId)
                throw new ForbiddenException("You may only modify tickets assigned to you.");
            return;
        }

        if (ticket.CreatedByUserId != currentUser.UserId)
            throw new ForbiddenException("You may only modify your own tickets.");
        if (ticket.Status != TicketStatus.New)
            throw new ConflictException("A ticket can only be edited by its requester while it is still New.");
    }

    /// <summary>Changing status: managers and admins anywhere, support agents only on their own tickets.</summary>
    private void EnsureCanWorkOn(Ticket ticket)
    {
        if (currentUser.IsInRole(RoleNames.SupportManager, RoleNames.Admin)) return;

        if (currentUser.IsInRole(RoleNames.SupportAgent) && ticket.AssignedToUserId == currentUser.UserId) return;

        throw new ForbiddenException("You may only change the status of tickets assigned to you.");
    }

    private void AddHistory(int ticketId, int? userId, string field, string? oldValue, string? newValue,
        DateTime now, string? note = null)
    {
        db.TicketHistory.Add(new TicketHistoryEntry
        {
            TicketId = ticketId, ChangedByUserId = userId,
            Field = field, OldValue = oldValue, NewValue = newValue, Note = note, CreatedAt = now
        });
    }

    /// <summary>Generates the next human-facing ticket number. Uniqueness is also enforced by a DB constraint.</summary>
    private async Task<string> NextTicketNumberAsync(CancellationToken ct)
    {
        var last = await db.Tickets.OrderByDescending(t => t.Id).Select(t => t.Id).FirstOrDefaultAsync(ct);
        return $"TKT-{last + 1:D6}";
    }

    internal static TicketListItemDto ToListItem(Ticket t, DateTime now) =>
        new(t.Id, t.TicketNumber, t.Title, t.Category.Name, t.Status, t.Priority,
            t.CreatedByUser.FullName, t.AssignedToUser?.FullName, t.AssignedToUserId,
            t.SlaDueAt, SlaCalculator.GetState(t, now), t.IsEscalated, t.CreatedAt, t.UpdatedAt);
}
