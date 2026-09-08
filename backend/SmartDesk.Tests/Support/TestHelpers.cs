using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;
using SmartDesk.Infrastructure.Persistence;

namespace SmartDesk.Tests.Support;

/// <summary>Fixed clock so SLA, timeout and escalation logic is deterministic in tests.</summary>
public sealed class FakeClock(DateTime now) : IClock
{
    public DateTime UtcNow { get; set; } = now;
    public static FakeClock Default => new(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
}

/// <summary>Stub principal, so services can be tested without an HTTP request.</summary>
public sealed class FakeCurrentUser(int? userId, string? role) : ICurrentUser
{
    public int? UserId { get; set; } = userId;
    public string? Email => $"user{UserId}@test.local";
    public string? Role { get; set; } = role;
    public bool IsAuthenticated => UserId is not null;
    public bool IsInRole(params string[] roles) => Role is not null && roles.Contains(Role);
}

public sealed class RecordingAudit : IAuditService
{
    public List<(string EntityType, string EntityId, string Action)> Entries { get; } = [];

    public Task LogAsync(string entityType, object entityId, string action,
        ActorType actorType = ActorType.User, int? actorUserId = null,
        object? details = null, CancellationToken ct = default)
    {
        Entries.Add((entityType, entityId.ToString()!, action));
        return Task.CompletedTask;
    }
}

public sealed class RecordingNotifications : INotificationService
{
    public List<(int UserId, string Subject)> Sent { get; } = [];

    public Task NotifyAsync(int userId, int? ticketId, string subject, string body, CancellationToken ct = default)
    {
        Sent.Add((userId, subject));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Builds an in-memory database pre-populated with a small, predictable world:
/// 4 roles, 1 manager, 2 support agents, 1 employee, 3 categories, 2 articles.
/// Used by the fast unit tests; the integration tests use real PostgreSQL instead.
/// </summary>
public static class TestDb
{
    public const int ManagerId = 1;
    public const int AgentAId = 2;
    public const int AgentBId = 3;
    public const int EmployeeId = 4;
    public const int OtherEmployeeId = 5;

    public const int NetworkCategoryId = 1;
    public const int HardwareCategoryId = 2;
    public const int GeneralCategoryId = 3;

    public static AppDbContext Create(DateTime now)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"smartdesk-{Guid.NewGuid()}")
            // The in-memory provider has no transactions. The approval executor opens one, and its
            // real atomicity is asserted against actual PostgreSQL in the integration tests, so here
            // we let the no-op transaction pass rather than failing on the warning.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);
        Seed(db, now);
        return db;
    }

    private static void Seed(AppDbContext db, DateTime now)
    {
        var roles = RoleNames.All
            .Select((name, i) => new Role { Id = i + 1, Name = name })
            .ToList();
        db.Roles.AddRange(roles);

        int RoleId(string name) => roles.First(r => r.Name == name).Id;

        db.Users.AddRange(
            new User { Id = ManagerId, Email = "manager@test.local", FullName = "Morgan Manager", PasswordHash = "x", RoleId = RoleId(RoleNames.SupportManager), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new User { Id = AgentAId, Email = "agenta@test.local", FullName = "Agent A", PasswordHash = "x", RoleId = RoleId(RoleNames.SupportAgent), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new User { Id = AgentBId, Email = "agentb@test.local", FullName = "Agent B", PasswordHash = "x", RoleId = RoleId(RoleNames.SupportAgent), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new User { Id = EmployeeId, Email = "emp@test.local", FullName = "Employee One", PasswordHash = "x", RoleId = RoleId(RoleNames.Employee), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new User { Id = OtherEmployeeId, Email = "emp2@test.local", FullName = "Employee Two", PasswordHash = "x", RoleId = RoleId(RoleNames.Employee), IsActive = true, CreatedAt = now, UpdatedAt = now });

        db.TicketCategories.AddRange(
            new TicketCategory { Id = NetworkCategoryId, Name = "Network", DefaultSlaHours = 8, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TicketCategory { Id = HardwareCategoryId, Name = "Hardware", DefaultSlaHours = 24, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TicketCategory { Id = GeneralCategoryId, Name = "General", DefaultSlaHours = 48, IsActive = true, CreatedAt = now, UpdatedAt = now });

        // Agent A is the network specialist; Agent B is the hardware specialist.
        db.SupportAgentSkills.AddRange(
            new SupportAgentSkill { Id = 1, UserId = AgentAId, CategoryId = NetworkCategoryId, ProficiencyLevel = 5, CreatedAt = now, UpdatedAt = now },
            new SupportAgentSkill { Id = 2, UserId = AgentBId, CategoryId = NetworkCategoryId, ProficiencyLevel = 2, CreatedAt = now, UpdatedAt = now },
            new SupportAgentSkill { Id = 3, UserId = AgentBId, CategoryId = HardwareCategoryId, ProficiencyLevel = 5, CreatedAt = now, UpdatedAt = now });

        db.KnowledgeArticles.AddRange(
            new KnowledgeArticle { Id = 1, Title = "Resolving VPN connection failures", Body = "Restart the VPN client and verify credentials before escalating.", CategoryId = NetworkCategoryId, Tags = ["vpn", "connection"], IsPublished = true, CreatedAt = now, UpdatedAt = now },
            new KnowledgeArticle { Id = 2, Title = "Replacing a faulty laptop battery", Body = "Book the device in for a hardware swap after confirming the charger works.", CategoryId = HardwareCategoryId, Tags = ["laptop", "battery"], IsPublished = true, CreatedAt = now, UpdatedAt = now },
            new KnowledgeArticle { Id = 3, Title = "Unpublished draft", Body = "This article is not published and must never reach an agent or an employee.", CategoryId = NetworkCategoryId, Tags = [], IsPublished = false, CreatedAt = now, UpdatedAt = now });

        db.SaveChanges();
    }

    public static Ticket AddTicket(
        AppDbContext db, DateTime createdAt, DateTime slaDueAt,
        TicketStatus status = TicketStatus.New,
        TicketPriority priority = TicketPriority.Medium,
        int categoryId = NetworkCategoryId,
        int createdBy = EmployeeId,
        int? assignedTo = null,
        string title = "VPN will not connect",
        string description = "The VPN client fails to connect after a password change.")
    {
        var id = db.Tickets.Count() + 1;
        var ticket = new Ticket
        {
            Id = id,
            TicketNumber = $"TKT-{id:D6}",
            Title = title,
            Description = description,
            CategoryId = categoryId,
            Status = status,
            Priority = priority,
            CreatedByUserId = createdBy,
            AssignedToUserId = assignedTo,
            SlaDueAt = slaDueAt,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }
}
