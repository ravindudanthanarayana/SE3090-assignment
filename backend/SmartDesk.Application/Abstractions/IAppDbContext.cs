using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SmartDesk.Domain.Entities;

namespace SmartDesk.Application.Abstractions;

/// <summary>
/// The data-access abstraction required by spec section 5.1.
/// EF Core's DbContext already is a unit-of-work plus repository; exposing it behind this interface
/// keeps the Application layer free of an Infrastructure reference and keeps services testable,
/// without inventing a per-entity repository over every DbSet. See ADR-006.
/// </summary>
public interface IAppDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketHistoryEntry> TicketHistory { get; }
    DbSet<TicketAssignment> TicketAssignments { get; }
    DbSet<SupportAgentSkill> SupportAgentSkills { get; }
    DbSet<KnowledgeArticle> KnowledgeArticles { get; }
    DbSet<TicketArticleLink> TicketArticleLinks { get; }
    DbSet<AgentWorkflow> AgentWorkflows { get; }
    DbSet<AgentStep> AgentSteps { get; }
    DbSet<AgentToolCall> AgentToolCalls { get; }
    DbSet<AiApproval> AiApprovals { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Used by the approval-execution path, which must be atomic (spec section 6.4).</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    DatabaseFacade Database { get; }
}
