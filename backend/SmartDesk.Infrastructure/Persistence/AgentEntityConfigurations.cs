using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDesk.Domain.Entities;

namespace SmartDesk.Infrastructure.Persistence;

public sealed class AgentWorkflowConfig : IEntityTypeConfiguration<AgentWorkflow>
{
    public void Configure(EntityTypeBuilder<AgentWorkflow> b)
    {
        b.ToTable("AgentWorkflows");
        b.HasKey(w => w.Id);
        b.Property(w => w.Objective).HasMaxLength(500).IsRequired();
        b.Property(w => w.CurrentStep).HasMaxLength(60);
        b.Property(w => w.ErrorMessage).HasMaxLength(2000);

        // jsonb: the plan and outcome shapes evolve with the agents, and PostgreSQL can still
        // query inside them without six extra tables. See ADR-003.
        b.Property(w => w.PlanJson).HasColumnType("jsonb");
        b.Property(w => w.FinalOutcomeJson).HasColumnType("jsonb");

        b.HasIndex(w => w.TicketId);
        b.HasIndex(w => w.Status);

        b.HasOne(w => w.Ticket).WithMany(t => t.Workflows)
            .HasForeignKey(w => w.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(w => w.StartedByUser).WithMany()
            .HasForeignKey(w => w.StartedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AgentStepConfig : IEntityTypeConfiguration<AgentStep>
{
    public void Configure(EntityTypeBuilder<AgentStep> b)
    {
        b.ToTable("AgentSteps");
        b.HasKey(s => s.Id);
        b.Property(s => s.AgentName).HasMaxLength(60).IsRequired();
        b.Property(s => s.Purpose).HasMaxLength(300);
        b.Property(s => s.ErrorMessage).HasMaxLength(2000);

        b.Property(s => s.InputJson).HasColumnType("jsonb");
        b.Property(s => s.OutputJson).HasColumnType("jsonb");
        b.Property(s => s.ValidationJson).HasColumnType("jsonb");

        // The workflow timeline reads steps in order for one workflow.
        b.HasIndex(s => new { s.WorkflowId, s.StepOrder });

        b.HasOne(s => s.Workflow).WithMany(w => w.Steps)
            .HasForeignKey(s => s.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AgentToolCallConfig : IEntityTypeConfiguration<AgentToolCall>
{
    public void Configure(EntityTypeBuilder<AgentToolCall> b)
    {
        b.ToTable("AgentToolCalls");
        b.HasKey(c => c.Id);
        b.Property(c => c.ToolName).HasMaxLength(60).IsRequired();
        b.Property(c => c.ErrorMessage).HasMaxLength(2000);
        b.Property(c => c.InputJson).HasColumnType("jsonb");
        b.Property(c => c.OutputJson).HasColumnType("jsonb");

        b.HasIndex(c => c.WorkflowId);
        b.HasIndex(c => c.StepId);

        b.HasOne(c => c.Workflow).WithMany(w => w.ToolCalls)
            .HasForeignKey(c => c.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        // A tool call can outlive its step row conceptually, so this is SetNull rather than Cascade.
        b.HasOne(c => c.Step).WithMany(s => s.ToolCalls)
            .HasForeignKey(c => c.StepId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AiApprovalConfig : IEntityTypeConfiguration<AiApproval>
{
    public void Configure(EntityTypeBuilder<AiApproval> b)
    {
        b.ToTable("AiApprovals", t =>
            t.HasCheckConstraint("CK_AiApprovals_Status", "\"Status\" BETWEEN 0 AND 3"));
        b.HasKey(a => a.Id);
        b.Property(a => a.Reason).HasMaxLength(1000).IsRequired();
        b.Property(a => a.DecisionNote).HasMaxLength(1000);
        b.Property(a => a.ProposedActionJson).HasColumnType("jsonb").IsRequired();

        // The Approval Center's "pending" feed is the hottest query on this table.
        b.HasIndex(a => a.Status);
        b.HasIndex(a => a.WorkflowId);
        b.HasIndex(a => a.TicketId);

        b.HasOne(a => a.Workflow).WithMany(w => w.Approvals)
            .HasForeignKey(a => a.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.Ticket).WithMany()
            .HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.DecidedByUser).WithMany()
            .HasForeignKey(a => a.DecidedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(a => a.Id);
        b.Property(a => a.EntityType).HasMaxLength(60).IsRequired();
        b.Property(a => a.EntityId).HasMaxLength(60).IsRequired();
        b.Property(a => a.Action).HasMaxLength(60).IsRequired();
        b.Property(a => a.DetailsJson).HasColumnType("jsonb");

        b.HasIndex(a => new { a.EntityType, a.EntityId });
        b.HasIndex(a => a.CreatedAt).IsDescending();

        b.HasOne(a => a.ActorUser).WithMany()
            .HasForeignKey(a => a.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(n => n.Id);
        b.Property(n => n.Channel).HasMaxLength(20).IsRequired();
        b.Property(n => n.Recipient).HasMaxLength(200).IsRequired();
        b.Property(n => n.Subject).HasMaxLength(200).IsRequired();
        b.Property(n => n.Body).HasMaxLength(4000).IsRequired();
        b.Property(n => n.Provider).HasMaxLength(40).IsRequired();
        b.Property(n => n.ProviderMessageId).HasMaxLength(200);
        b.Property(n => n.ErrorMessage).HasMaxLength(1000);

        b.HasIndex(n => n.UserId);
        b.HasIndex(n => n.Status);

        b.HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(n => n.Ticket).WithMany()
            .HasForeignKey(n => n.TicketId).OnDelete(DeleteBehavior.SetNull);
    }
}
