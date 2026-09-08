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
/// Component C - Knowledge Base and Solutions (Student 3).
/// Owns article CRUD plus two business operations: ticket-aware relevance ranking and linking an
/// article to a ticket as a suggested solution.
/// </summary>
public sealed class KnowledgeService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditService audit,
    IClock clock)
{
    private const int ExcerptLength = 200;

    public async Task<PagedResult<ArticleListItemDto>> QueryAsync(ArticleQuery query, CancellationToken ct = default)
    {
        var q = db.KnowledgeArticles.AsNoTracking().Include(a => a.Category).AsQueryable();

        // Employees only ever see published articles, enforced in the query.
        if (currentUser.IsInRole(RoleNames.Employee))
            q = q.Where(a => a.IsPublished);
        else if (query.IsPublished.HasValue)
            q = q.Where(a => a.IsPublished == query.IsPublished);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(a => a.Title.ToLower().Contains(term) || a.Body.ToLower().Contains(term));
        }

        if (query.CategoryId.HasValue) q = q.Where(a => a.CategoryId == query.CategoryId);

        q = (query.SortBy?.ToLowerInvariant(), query.Descending) switch
        {
            ("title", true) => q.OrderByDescending(a => a.Title),
            ("title", false) => q.OrderBy(a => a.Title),
            ("viewcount", true) => q.OrderByDescending(a => a.ViewCount),
            ("viewcount", false) => q.OrderBy(a => a.ViewCount),
            ("createdat", false) => q.OrderBy(a => a.CreatedAt),
            ("createdat", true) => q.OrderByDescending(a => a.CreatedAt),
            (_, false) => q.OrderBy(a => a.UpdatedAt),
            _ => q.OrderByDescending(a => a.UpdatedAt)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        return new PagedResult<ArticleListItemDto>(
            items.Select(a => new ArticleListItemDto(
                a.Id, a.Title, Excerpt(a.Body), a.CategoryId, a.Category.Name,
                a.Tags, a.IsPublished, a.ViewCount, a.UpdatedAt)).ToList(),
            query.Page, query.PageSize, total);
    }

    public async Task<ArticleDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var article = await db.KnowledgeArticles
            .Include(a => a.Category).Include(a => a.AuthorUser)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("KnowledgeArticle", id);

        if (!article.IsPublished && currentUser.IsInRole(RoleNames.Employee))
            throw new ForbiddenException("This article is not published.");

        article.ViewCount++;
        await db.SaveChangesAsync(ct);

        return ToDetail(article);
    }

    public async Task<ArticleDetailDto> CreateAsync(UpsertArticleRequest request, CancellationToken ct = default)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        var now = clock.UtcNow;
        var article = new KnowledgeArticle
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            CategoryId = request.CategoryId,
            Tags = NormalizeTags(request.Tags),
            IsPublished = request.IsPublished,
            AuthorUserId = currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.KnowledgeArticles.Add(article);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("KnowledgeArticle", article.Id, "ArticleCreated", ActorType.User, currentUser.UserId,
            new { article.Title }, ct);

        return await LoadDetailAsync(article.Id, ct);
    }

    public async Task<ArticleDetailDto> UpdateAsync(int id, UpsertArticleRequest request, CancellationToken ct = default)
    {
        var article = await db.KnowledgeArticles.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("KnowledgeArticle", id);

        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        article.Title = request.Title.Trim();
        article.Body = request.Body.Trim();
        article.CategoryId = request.CategoryId;
        article.Tags = NormalizeTags(request.Tags);
        article.IsPublished = request.IsPublished;
        article.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("KnowledgeArticle", id, "ArticleUpdated", ActorType.User, currentUser.UserId, null, ct);

        return await LoadDetailAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var article = await db.KnowledgeArticles.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("KnowledgeArticle", id);

        db.KnowledgeArticles.Remove(article);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("KnowledgeArticle", id, "ArticleDeleted", ActorType.User, currentUser.UserId, null, ct);
    }

    /// <summary>
    /// Business operation beyond CRUD: rank published articles against a specific ticket.
    /// Uses the same ArticleRelevance scorer as the Solution agent's SearchKnowledgeBase tool.
    /// </summary>
    public async Task<IReadOnlyList<RelevantArticleDto>> GetRelevantAsync(int ticketId, int take = 5, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        var candidates = await db.KnowledgeArticles.AsNoTracking()
            .Where(a => a.IsPublished)
            .Include(a => a.Category)
            .ToListAsync(ct);

        var scored = ArticleRelevance.Score(
            $"{ticket.Title} {ticket.Description}",
            ticket.CategoryId,
            candidates.Select(a => new ArticleCandidate(a.Id, a.Title, a.Body, a.CategoryId, a.Tags)));

        var byId = candidates.ToDictionary(a => a.Id);
        return scored.Take(take)
            .Select(s => new RelevantArticleDto(
                s.Id, byId[s.Id].Title, Excerpt(byId[s.Id].Body),
                byId[s.Id].Category.Name, s.Score, s.MatchReason))
            .ToList();
    }

    /// <summary>Business operation beyond CRUD: attach an article to a ticket as a suggested solution.</summary>
    public async Task<LinkedArticleDto> LinkToTicketAsync(int ticketId, LinkArticleRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        var article = await db.KnowledgeArticles.FirstOrDefaultAsync(a => a.Id == request.ArticleId, ct)
            ?? throw new NotFoundException("KnowledgeArticle", request.ArticleId);

        if (await db.TicketArticleLinks.AnyAsync(l => l.TicketId == ticketId && l.ArticleId == article.Id, ct))
            throw new ConflictException("This article is already linked to the ticket.");

        var link = new TicketArticleLink
        {
            TicketId = ticketId,
            ArticleId = article.Id,
            Source = ArticleLinkSource.Manual,
            RelevanceScore = 1.0,
            CreatedAt = clock.UtcNow
        };
        db.TicketArticleLinks.Add(link);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("Ticket", ticketId, "ArticleLinked", ActorType.User, currentUser.UserId,
            new { articleId = article.Id }, ct);

        return new LinkedArticleDto(article.Id, article.Title, link.RelevanceScore, link.Source);
    }

    // ---- Helpers -----------------------------------------------------------------------

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        if (!await db.TicketCategories.AnyAsync(c => c.Id == categoryId, ct))
            throw new ValidationException($"Category {categoryId} does not exist.");
    }

    private async Task<ArticleDetailDto> LoadDetailAsync(int id, CancellationToken ct)
    {
        var article = await db.KnowledgeArticles.AsNoTracking()
            .Include(a => a.Category).Include(a => a.AuthorUser)
            .FirstAsync(a => a.Id == id, ct);
        return ToDetail(article);
    }

    private static ArticleDetailDto ToDetail(KnowledgeArticle a) =>
        new(a.Id, a.Title, a.Body, a.CategoryId, a.Category.Name, a.Tags, a.IsPublished,
            a.ViewCount, a.AuthorUser?.FullName, a.CreatedAt, a.UpdatedAt);

    private static string Excerpt(string body) =>
        body.Length <= ExcerptLength ? body : body[..ExcerptLength] + "...";

    private static string[] NormalizeTags(string[] tags) =>
        tags.Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .Take(10)
            .ToArray();
}
