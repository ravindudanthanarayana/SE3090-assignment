using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;
using SmartDesk.Tests.Support;

namespace SmartDesk.Tests.Integration;

/// <summary>
/// PostgreSQL integration tests (spec section 12): migrations, real constraints, relationships and
/// transaction behaviour. These assert that the database itself enforces our invariants, not just
/// the C# service layer.
/// </summary>
[Collection("postgres")]
public class DatabaseIntegrationTests(PostgresFixture fixture)
{
    // ---- Migrations -------------------------------------------------------------------------

    [Fact]
    public async Task All_migrations_apply_and_none_is_left_pending()
    {
        await using var db = fixture.CreateContext();

        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Seed_data_is_present_and_coherent()
    {
        await using var db = fixture.CreateContext();

        // Reference data is fixed by the seeder and no test adds to it.
        Assert.Equal(4, await db.Roles.CountAsync());
        Assert.Equal(8, await db.Users.CountAsync());
        Assert.Equal(6, await db.TicketCategories.CountAsync());

        // Tickets and articles are appended to by other tests sharing this database, so the
        // assertion is that the seeded baseline is present, not that nothing was added since.
        Assert.True(await db.KnowledgeArticles.CountAsync() >= 10);
        Assert.True(await db.Tickets.CountAsync() >= 20);

        // Every seeded role is one the application actually uses.
        var roleNames = await db.Roles.Select(r => r.Name).ToListAsync();
        Assert.All(roleNames, n => Assert.Contains(n, RoleNames.All));

        // Every ticket points at a category and a creator that actually exist.
        Assert.False(await db.Tickets.AnyAsync(t => t.Category == null || t.CreatedByUser == null));
    }

    // ---- Constraints ------------------------------------------------------------------------

    [Fact]
    public async Task Duplicate_email_is_rejected_by_the_unique_index()
    {
        await using var db = fixture.CreateContext();
        var roleId = await db.Roles.Where(r => r.Name == RoleNames.Employee).Select(r => r.Id).FirstAsync();

        db.Users.Add(new User
        {
            Email = "admin@smartdesk.local", // already seeded
            FullName = "Impostor", PasswordHash = "x", RoleId = roleId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23505", ((PostgresException)ex.InnerException!).SqlState); // unique_violation
    }

    [Fact]
    public async Task Duplicate_ticket_number_is_rejected_by_the_unique_index()
    {
        await using var db = fixture.CreateContext();
        var existing = await db.Tickets.AsNoTracking().FirstAsync();

        db.Tickets.Add(new Ticket
        {
            TicketNumber = existing.TicketNumber,
            Title = "Duplicate number", Description = "should fail",
            CategoryId = existing.CategoryId, CreatedByUserId = existing.CreatedByUserId,
            SlaDueAt = DateTime.UtcNow.AddHours(8), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Skill_proficiency_outside_one_to_five_is_rejected_by_the_check_constraint()
    {
        await using var db = fixture.CreateContext();
        var agentId = await db.Users.Where(u => u.Role.Name == RoleNames.SupportAgent).Select(u => u.Id).FirstAsync();
        var categoryId = await db.TicketCategories.OrderByDescending(c => c.Id).Select(c => c.Id).FirstAsync();

        db.SupportAgentSkills.Add(new SupportAgentSkill
        {
            UserId = agentId, CategoryId = categoryId, ProficiencyLevel = 99,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal("23514", ((PostgresException)ex.InnerException!).SqlState); // check_violation
    }

    [Fact]
    public async Task One_skill_row_per_agent_per_category_is_enforced()
    {
        await using var db = fixture.CreateContext();
        var existing = await db.SupportAgentSkills.AsNoTracking().FirstAsync();

        db.SupportAgentSkills.Add(new SupportAgentSkill
        {
            UserId = existing.UserId, CategoryId = existing.CategoryId, ProficiencyLevel = 1,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_ticket_cannot_reference_a_category_that_does_not_exist()
    {
        await using var db = fixture.CreateContext();
        var creatorId = await db.Users.Select(u => u.Id).FirstAsync();

        db.Tickets.Add(new Ticket
        {
            TicketNumber = $"TKT-{Random.Shared.Next(900000, 999999)}",
            Title = "Orphan", Description = "no such category",
            CategoryId = 999_999, CreatedByUserId = creatorId,
            SlaDueAt = DateTime.UtcNow.AddHours(8), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal("23503", ((PostgresException)ex.InnerException!).SqlState); // foreign_key_violation
    }

    // ---- Relationships ------------------------------------------------------------------------

    [Fact]
    public async Task Deleting_a_ticket_cascades_to_its_comments_and_history()
    {
        await using var db = fixture.CreateContext();
        var ticket = await NewTicketAsync(db);

        db.TicketComments.Add(new TicketComment
        {
            TicketId = ticket.Id, AuthorUserId = ticket.CreatedByUserId,
            Body = "A comment", CreatedAt = DateTime.UtcNow
        });
        db.TicketHistory.Add(new TicketHistoryEntry
        {
            TicketId = ticket.Id, Field = "Status", NewValue = "New", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync();

        Assert.False(await db.TicketComments.AnyAsync(c => c.TicketId == ticket.Id));
        Assert.False(await db.TicketHistory.AnyAsync(h => h.TicketId == ticket.Id));
    }

    [Fact]
    public async Task A_category_that_still_has_tickets_cannot_be_deleted()
    {
        await using var db = fixture.CreateContext();
        var categoryId = await db.Tickets.Select(t => t.CategoryId).FirstAsync();
        var category = await db.TicketCategories.FirstAsync(c => c.Id == categoryId);

        db.TicketCategories.Remove(category);

        // The FK is RESTRICT, so history is protected even if application code got it wrong.
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Jsonb_agent_state_round_trips_correctly()
    {
        await using var db = fixture.CreateContext();
        var ticket = await NewTicketAsync(db);

        const string plan = """{"steps":[{"order":1,"agent":"TriageAgent"}],"rationale":"test"}""";
        var workflow = new AgentWorkflow
        {
            TicketId = ticket.Id, Objective = "test objective", Status = WorkflowStatus.Running,
            PlanJson = plan, StartedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await db.AgentWorkflows.AsNoTracking().FirstAsync(w => w.Id == workflow.Id);
        using var doc = System.Text.Json.JsonDocument.Parse(loaded.PlanJson!);
        Assert.Equal("test", doc.RootElement.GetProperty("rationale").GetString());
    }

    [Fact]
    public async Task Postgres_array_column_round_trips_article_tags()
    {
        await using var db = fixture.CreateContext();
        var categoryId = await db.TicketCategories.Select(c => c.Id).FirstAsync();

        var article = new KnowledgeArticle
        {
            Title = "Array round trip", Body = new string('x', 30), CategoryId = categoryId,
            Tags = ["alpha", "beta", "gamma"], IsPublished = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.KnowledgeArticles.Add(article);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await db.KnowledgeArticles.AsNoTracking().FirstAsync(a => a.Id == article.Id);
        Assert.Equal(["alpha", "beta", "gamma"], loaded.Tags);
    }

    // ---- Transactions --------------------------------------------------------------------------

    [Fact]
    public async Task The_approval_execution_is_atomic_when_a_later_step_fails()
    {
        await using var db = fixture.CreateContext();
        var ticket = await NewTicketAsync(db);
        var manager = await db.Users.FirstAsync(u => u.Role.Name == RoleNames.SupportManager);

        var workflow = new AgentWorkflow
        {
            TicketId = ticket.Id, Objective = "atomicity test", Status = WorkflowStatus.AwaitingApproval,
            StartedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var approval = new AiApproval
        {
            WorkflowId = workflow.Id, TicketId = ticket.Id,
            ActionType = ApprovalActionType.Assign,
            // A target user that no longer qualifies: the executor must reject it mid-transaction.
            ProposedActionJson = $$"""{"actionType":1,"ticketId":{{ticket.Id}},"targetUserId":999999,"description":"x"}""",
            Reason = "Assign to a user who does not exist",
            Status = ApprovalStatus.Approved,
            DecidedByUserId = manager.Id, DecidedAt = DateTime.UtcNow,
            RequestedAt = DateTime.UtcNow
        };
        db.AiApprovals.Add(approval);
        await db.SaveChangesAsync();

        var executor = new ApprovalActionExecutor(db, new RecordingAudit(), new RecordingNotifications(),
            new FakeClock(DateTime.UtcNow));

        await Assert.ThrowsAnyAsync<Exception>(() => executor.ExecuteAsync(approval));

        db.ChangeTracker.Clear();
        var after = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);

        // Nothing was half-applied.
        Assert.Null(after.AssignedToUserId);
        Assert.Equal(TicketStatus.New, after.Status);
        Assert.False(await db.TicketAssignments.AnyAsync(a => a.TicketId == ticket.Id));
    }

    [Fact]
    public async Task An_approved_escalation_writes_ticket_history_and_assignment_together()
    {
        await using var db = fixture.CreateContext();
        var ticket = await NewTicketAsync(db);
        var manager = await db.Users.FirstAsync(u => u.Role.Name == RoleNames.SupportManager);

        var workflow = new AgentWorkflow
        {
            TicketId = ticket.Id, Objective = "escalation test", Status = WorkflowStatus.AwaitingApproval,
            StartedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var approval = new AiApproval
        {
            WorkflowId = workflow.Id, TicketId = ticket.Id,
            ActionType = ApprovalActionType.Escalate,
            ProposedActionJson = $$"""{"actionType":0,"ticketId":{{ticket.Id}},"description":"SLA at risk"}""",
            Reason = "The SLA is at risk.", Status = ApprovalStatus.Approved,
            DecidedByUserId = manager.Id, DecidedAt = DateTime.UtcNow, RequestedAt = DateTime.UtcNow
        };
        db.AiApprovals.Add(approval);
        await db.SaveChangesAsync();

        var executor = new ApprovalActionExecutor(db, new RecordingAudit(), new RecordingNotifications(),
            new FakeClock(DateTime.UtcNow));
        await executor.ExecuteAsync(approval);

        db.ChangeTracker.Clear();
        var after = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);

        Assert.Equal(TicketStatus.Escalated, after.Status);
        Assert.True(after.IsEscalated);
        Assert.NotNull(after.EscalatedAt);
        Assert.True(await db.TicketHistory.AnyAsync(h => h.TicketId == ticket.Id && h.Field == "Status"));
    }

    private static async Task<Ticket> NewTicketAsync(Microsoft.EntityFrameworkCore.DbContext context)
    {
        var db = (SmartDesk.Infrastructure.Persistence.AppDbContext)context;
        var category = await db.TicketCategories.FirstAsync();
        var employee = await db.Users.FirstAsync(u => u.Role.Name == RoleNames.Employee);

        var ticket = new Ticket
        {
            TicketNumber = $"TKT-{Random.Shared.Next(500000, 899999)}",
            Title = "Integration test ticket",
            Description = "Created by an automated integration test.",
            CategoryId = category.Id,
            CreatedByUserId = employee.Id,
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            SlaDueAt = DateTime.UtcNow.AddHours(8),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket;
    }
}
