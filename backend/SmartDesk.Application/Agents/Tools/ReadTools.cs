using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.BusinessRules;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Agents.Tools;

/// <summary>Reads the one ticket this workflow is about. Cannot read any other ticket.</summary>
public sealed class GetTicketTool(IAppDbContext db, IClock clock) : IAgentTool
{
    public string Name => "GetTicket";
    public string Description => "Returns the ticket this workflow was started for.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        // Input validation: the agent may only ask for the workflow's own ticket.
        if (input.TryGetProperty("ticketId", out var idProp) &&
            idProp.ValueKind == JsonValueKind.Number &&
            idProp.GetInt32() != context.TicketId)
        {
            return ToolResult.Fail("This tool may only read the ticket the workflow was started for.");
        }

        var ticket = await db.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == context.TicketId, ct);

        if (ticket is null) return ToolResult.Fail($"Ticket {context.TicketId} not found.");

        return ToolResult.Ok(new
        {
            ticketId = ticket.Id,
            ticketNumber = ticket.TicketNumber,
            title = ticket.Title,
            description = ticket.Description,
            currentCategory = ticket.Category.Name,
            currentCategoryId = ticket.CategoryId,
            currentPriority = ticket.Priority.ToString(),
            status = ticket.Status.ToString(),
            createdAt = ticket.CreatedAt,
            slaDueAt = ticket.SlaDueAt,
            hoursUntilSlaDue = SlaCalculator.HoursRemaining(ticket, clock.UtcNow)
        });
    }
}

/// <summary>
/// Searches the knowledge base. Returns published articles only, ranked by our own deterministic
/// relevance function - the agent cannot influence the ranking, only read the result.
/// </summary>
public sealed class SearchKnowledgeBaseTool(IAppDbContext db) : IAgentTool
{
    private const int MaxQueryLength = 200;
    private const int MaxResults = 5;

    public string Name => "SearchKnowledgeBase";
    public string Description => "Searches published knowledge articles by keywords and optional category.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        if (!input.TryGetProperty("query", out var q) || q.ValueKind != JsonValueKind.String)
            return ToolResult.Fail("Input must include a string 'query'.");

        var query = q.GetString() ?? string.Empty;
        if (query.Length > MaxQueryLength) query = query[..MaxQueryLength];
        // Strip control characters so a crafted query cannot corrupt logs or downstream parsing.
        query = new string(query.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (query.Length == 0) return ToolResult.Fail("'query' must not be empty.");

        int? categoryId = input.TryGetProperty("categoryId", out var c) && c.ValueKind == JsonValueKind.Number
            ? c.GetInt32()
            : null;

        var candidates = await db.KnowledgeArticles
            .AsNoTracking()
            .Where(a => a.IsPublished)
            .Select(a => new ArticleCandidate(a.Id, a.Title, a.Body, a.CategoryId, a.Tags))
            .ToListAsync(ct);

        var scored = ArticleRelevance.Score(query, categoryId, candidates).Take(MaxResults).ToList();
        var byId = candidates.ToDictionary(a => a.Id);

        return ToolResult.Ok(new
        {
            query,
            results = scored.Select(s => new
            {
                articleId = s.Id,
                title = byId[s.Id].Title,
                // Excerpt only - the agent never needs, and never receives, the full article body.
                excerpt = byId[s.Id].Body.Length > 300 ? byId[s.Id].Body[..300] + "..." : byId[s.Id].Body,
                relevanceScore = s.Score,
                matchReason = s.MatchReason
            })
        });
    }
}

/// <summary>Lists active support agents. Returns names and skills only - never emails or password hashes.</summary>
public sealed class GetSupportAgentsTool(IAppDbContext db) : IAgentTool
{
    public string Name => "GetSupportAgents";
    public string Description => "Lists active support agents with their category skill levels.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        var agents = await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role.Name == RoleNames.SupportAgent)
            .Select(u => new
            {
                userId = u.Id,
                fullName = u.FullName,
                skills = u.Skills.Select(s => new
                {
                    categoryId = s.CategoryId,
                    categoryName = s.Category.Name,
                    proficiencyLevel = s.ProficiencyLevel
                })
            })
            .ToListAsync(ct);

        return ToolResult.Ok(new { count = agents.Count, agents });
    }
}

/// <summary>Returns aggregate open-work counts per support agent. Aggregates only, no ticket contents.</summary>
public sealed class GetAgentWorkloadTool(IAppDbContext db, IClock clock) : IAgentTool
{
    public string Name => "GetAgentWorkload";
    public string Description => "Returns current open and at-risk ticket counts for each support agent.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var open = TicketStatusMachine.OpenStatuses;

        var workload = await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role.Name == RoleNames.SupportAgent)
            .Select(u => new
            {
                userId = u.Id,
                fullName = u.FullName,
                openTickets = db.Tickets.Count(t => t.AssignedToUserId == u.Id && open.Contains(t.Status)),
                breaching = db.Tickets.Count(t => t.AssignedToUserId == u.Id && open.Contains(t.Status) && t.SlaDueAt < now)
            })
            .ToListAsync(ct);

        return ToolResult.Ok(new { workload });
    }
}

/// <summary>
/// Runs the deterministic assignment scorer. This is the same function the manager-facing
/// recommendation endpoint uses, so the agent and the API can never disagree. No LLM involved.
/// </summary>
public sealed class ScoreAssignmentCandidatesTool(IAppDbContext db) : IAgentTool
{
    public string Name => "ScoreAssignmentCandidates";
    public string Description => "Ranks support agents for this ticket by skill match and current workload.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == context.TicketId, ct);
        if (ticket is null) return ToolResult.Fail($"Ticket {context.TicketId} not found.");

        // An explicit categoryId lets the agent score against the category Triage proposed
        // rather than the one currently stored, but it must be a real category.
        var categoryId = ticket.CategoryId;
        if (input.TryGetProperty("categoryId", out var c) && c.ValueKind == JsonValueKind.Number)
        {
            var requested = c.GetInt32();
            if (await db.TicketCategories.AnyAsync(x => x.Id == requested, ct)) categoryId = requested;
        }

        var open = TicketStatusMachine.OpenStatuses;
        var candidates = await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role.Name == RoleNames.SupportAgent)
            .Select(u => new CandidateInput(
                u.Id,
                u.FullName,
                u.Skills.Where(s => s.CategoryId == categoryId).Select(s => s.ProficiencyLevel).FirstOrDefault(),
                db.Tickets.Count(t => t.AssignedToUserId == u.Id && open.Contains(t.Status))))
            .ToListAsync(ct);

        var ranked = AssignmentScorer.Rank(candidates);
        return ToolResult.Ok(new
        {
            categoryId,
            candidates = ranked.Select(r => new
            {
                userId = r.UserId,
                fullName = r.FullName,
                score = r.Score,
                skillLevel = r.SkillLevel,
                openTickets = r.OpenTicketCount,
                explanation = r.Explanation
            })
        });
    }
}

/// <summary>Reports the SLA position of this workflow's ticket. Read-only; the number is computed in C#.</summary>
public sealed class CheckSlaTool(IAppDbContext db, IClock clock) : IAgentTool
{
    public string Name => "CheckSla";
    public string Description => "Returns the SLA deadline, hours remaining and risk state for this ticket.";

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == context.TicketId, ct);
        if (ticket is null) return ToolResult.Fail($"Ticket {context.TicketId} not found.");

        var now = clock.UtcNow;
        var state = SlaCalculator.GetState(ticket, now);

        return ToolResult.Ok(new
        {
            ticketId = ticket.Id,
            slaDueAt = ticket.SlaDueAt,
            hoursRemaining = SlaCalculator.HoursRemaining(ticket, now),
            slaState = state.ToString(),
            isBreached = state == SlaState.Breached,
            isAtRisk = state == SlaState.AtRisk,
            currentPriority = ticket.Priority.ToString(),
            currentStatus = ticket.Status.ToString()
        });
    }
}
