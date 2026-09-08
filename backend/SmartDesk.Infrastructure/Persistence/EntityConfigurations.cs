using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Infrastructure.Persistence;

public sealed class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(r => r.Id);
        b.Property(r => r.Name).HasMaxLength(30).IsRequired();
        b.Property(r => r.Description).HasMaxLength(200);
        b.HasIndex(r => r.Name).IsUnique();
    }
}

public sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasMaxLength(200).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        b.Property(u => u.Department).HasMaxLength(100);

        // Email is the login identifier, so uniqueness is a database guarantee, not just a service check.
        b.HasIndex(u => u.Email).IsUnique();
        b.HasIndex(u => u.RoleId);

        // Restrict: a role that still has users must not be removable.
        b.HasOne(u => u.Role).WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TicketCategoryConfig : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> b)
    {
        b.ToTable("TicketCategories", t =>
            t.HasCheckConstraint("CK_TicketCategories_SlaHours", "\"DefaultSlaHours\" > 0"));
        b.HasKey(c => c.Id);
        b.Property(c => c.Name).HasMaxLength(100).IsRequired();
        b.Property(c => c.Description).HasMaxLength(500);
        b.HasIndex(c => c.Name).IsUnique();
    }
}

public sealed class TicketConfig : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> b)
    {
        b.ToTable("Tickets", t =>
        {
            // Enums are stored as int, so the database still constrains them to the known range.
            t.HasCheckConstraint("CK_Tickets_Status", "\"Status\" BETWEEN 0 AND 7");
            t.HasCheckConstraint("CK_Tickets_Priority", "\"Priority\" BETWEEN 0 AND 3");
        });

        b.HasKey(t => t.Id);
        b.Property(t => t.TicketNumber).HasMaxLength(20).IsRequired();
        b.Property(t => t.Title).HasMaxLength(200).IsRequired();
        b.Property(t => t.Description).HasMaxLength(5000).IsRequired();
        b.Property(t => t.Resolution).HasMaxLength(4000);
        b.Property(t => t.EscalationReason).HasMaxLength(1000);

        b.HasIndex(t => t.TicketNumber).IsUnique();
        b.HasIndex(t => t.Status);
        b.HasIndex(t => t.Priority);
        b.HasIndex(t => t.CategoryId);
        b.HasIndex(t => t.CreatedByUserId);
        b.HasIndex(t => t.AssignedToUserId);
        b.HasIndex(t => t.SlaDueAt);
        // The dashboard's hottest query filters on both at once.
        b.HasIndex(t => new { t.Status, t.Priority });

        b.HasOne(t => t.Category).WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(t => t.CreatedByUser).WithMany()
            .HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Unassigning on user deletion keeps the ticket, which matters for the audit trail.
        b.HasOne(t => t.AssignedToUser).WithMany()
            .HasForeignKey(t => t.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TicketCommentConfig : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> b)
    {
        b.ToTable("TicketComments");
        b.HasKey(c => c.Id);
        b.Property(c => c.Body).HasMaxLength(4000).IsRequired();
        b.HasIndex(c => c.TicketId);

        b.HasOne(c => c.Ticket).WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.AuthorUser).WithMany()
            .HasForeignKey(c => c.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TicketHistoryConfig : IEntityTypeConfiguration<TicketHistoryEntry>
{
    public void Configure(EntityTypeBuilder<TicketHistoryEntry> b)
    {
        b.ToTable("TicketHistory");
        b.HasKey(h => h.Id);
        b.Property(h => h.Field).HasMaxLength(60).IsRequired();
        b.Property(h => h.OldValue).HasMaxLength(200);
        b.Property(h => h.NewValue).HasMaxLength(200);
        b.Property(h => h.Note).HasMaxLength(2000);
        b.HasIndex(h => h.TicketId);

        b.HasOne(h => h.Ticket).WithMany(t => t.History)
            .HasForeignKey(h => h.TicketId).OnDelete(DeleteBehavior.Cascade);
        // Null actor is meaningful: it marks a system or approved-agent change.
        b.HasOne(h => h.ChangedByUser).WithMany()
            .HasForeignKey(h => h.ChangedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TicketAssignmentConfig : IEntityTypeConfiguration<TicketAssignment>
{
    public void Configure(EntityTypeBuilder<TicketAssignment> b)
    {
        b.ToTable("TicketAssignments");
        b.HasKey(a => a.Id);
        b.Property(a => a.Reason).HasMaxLength(1000);
        b.HasIndex(a => a.TicketId);
        b.HasIndex(a => a.AssignedToUserId);

        b.HasOne(a => a.Ticket).WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.AssignedToUser).WithMany()
            .HasForeignKey(a => a.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.AssignedByUser).WithMany()
            .HasForeignKey(a => a.AssignedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SupportAgentSkillConfig : IEntityTypeConfiguration<SupportAgentSkill>
{
    public void Configure(EntityTypeBuilder<SupportAgentSkill> b)
    {
        b.ToTable("SupportAgentSkills", t =>
            t.HasCheckConstraint("CK_SupportAgentSkills_Level", "\"ProficiencyLevel\" BETWEEN 1 AND 5"));
        b.HasKey(s => s.Id);

        // One skill row per agent per category.
        b.HasIndex(s => new { s.UserId, s.CategoryId }).IsUnique();

        b.HasOne(s => s.User).WithMany(u => u.Skills)
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(s => s.Category).WithMany()
            .HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class KnowledgeArticleConfig : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> b)
    {
        b.ToTable("KnowledgeArticles");
        b.HasKey(a => a.Id);
        b.Property(a => a.Title).HasMaxLength(200).IsRequired();
        b.Property(a => a.Body).HasMaxLength(20000).IsRequired();
        // PostgreSQL text[] avoids a join table for a purely descriptive attribute.
        b.Property(a => a.Tags).HasColumnType("text[]");
        b.HasIndex(a => a.CategoryId);
        b.HasIndex(a => a.IsPublished);

        b.HasOne(a => a.Category).WithMany()
            .HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.AuthorUser).WithMany()
            .HasForeignKey(a => a.AuthorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TicketArticleLinkConfig : IEntityTypeConfiguration<TicketArticleLink>
{
    public void Configure(EntityTypeBuilder<TicketArticleLink> b)
    {
        b.ToTable("TicketArticleLinks");
        b.HasKey(l => l.Id);
        b.HasIndex(l => new { l.TicketId, l.ArticleId }).IsUnique();

        b.HasOne(l => l.Ticket).WithMany(t => t.ArticleLinks)
            .HasForeignKey(l => l.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(l => l.Article).WithMany()
            .HasForeignKey(l => l.ArticleId).OnDelete(DeleteBehavior.Cascade);
    }
}
