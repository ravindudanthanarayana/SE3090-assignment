using SmartDesk.Domain.Enums;

namespace SmartDesk.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = [];
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SupportAgentSkill> Skills { get; set; } = [];
}

public class TicketCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Base SLA window for this category, before the priority multiplier is applied.</summary>
    public int DefaultSlaHours { get; set; } = 24;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = [];
}

public class Ticket
{
    public int Id { get; set; }
    /// <summary>Human-facing identifier, e.g. TKT-000042. Unique; searchable.</summary>
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public TicketCategory Category { get; set; } = null!;

    public TicketStatus Status { get; set; } = TicketStatus.New;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    /// <summary>Computed in C# from category SLA hours and priority. Never set by an agent.</summary>
    public DateTime SlaDueAt { get; set; }
    public bool IsEscalated { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public string? EscalationReason { get; set; }

    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = [];
    public ICollection<TicketHistoryEntry> History { get; set; } = [];
    public ICollection<TicketAssignment> Assignments { get; set; } = [];
    public ICollection<TicketArticleLink> ArticleLinks { get; set; } = [];
    public ICollection<TicketAttachment> Attachments { get; set; } = [];
    public ICollection<AgentWorkflow> Workflows { get; set; } = [];
}

/// <summary>A screenshot or photo supplied by the requester to make a support issue reproducible.</summary>
public class TicketAttachment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public string Body { get; set; } = string.Empty;
    /// <summary>Internal staff notes are never returned to an Employee.</summary>
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Field-level change log for a ticket. Satisfies the "history" requirement in spec section 5.4.</summary>
public class TicketHistoryEntry
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    /// <summary>Null when the change was made by the system or an approved agent action.</summary>
    public int? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TicketAssignment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int AssignedToUserId { get; set; }
    public User AssignedToUser { get; set; } = null!;
    public int? AssignedByUserId { get; set; }
    public User? AssignedByUser { get; set; }
    public string? Reason { get; set; }
    public AssignmentSource Source { get; set; } = AssignmentSource.Manual;
    public DateTime CreatedAt { get; set; }
}

public class SupportAgentSkill
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CategoryId { get; set; }
    public TicketCategory Category { get; set; } = null!;
    /// <summary>1 (novice) to 5 (expert). Feeds the deterministic assignment score.</summary>
    public int ProficiencyLevel { get; set; } = 3;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class KnowledgeArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public TicketCategory Category { get; set; } = null!;
    public string[] Tags { get; set; } = [];
    public bool IsPublished { get; set; } = true;
    public int ViewCount { get; set; }
    public int? AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TicketArticleLink
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int ArticleId { get; set; }
    public KnowledgeArticle Article { get; set; } = null!;
    public ArticleLinkSource Source { get; set; } = ArticleLinkSource.Manual;
    public double RelevanceScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
