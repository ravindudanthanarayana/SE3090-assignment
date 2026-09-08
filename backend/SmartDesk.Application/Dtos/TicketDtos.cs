using System.ComponentModel.DataAnnotations;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Dtos;

public sealed class CreateTicketRequest
{
    [Required, MinLength(5), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    /// <summary>Requester's own view of urgency. The Triage agent may propose a different priority.</summary>
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
}

public sealed class UpdateTicketRequest
{
    [Required, MinLength(5), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public TicketPriority Priority { get; set; }
}

public sealed class ChangeStatusRequest
{
    [Required]
    public TicketStatus Status { get; set; }

    [MaxLength(2000)]
    public string? Note { get; set; }

    /// <summary>Required when moving to Resolved.</summary>
    [MaxLength(4000)]
    public string? Resolution { get; set; }
}

public sealed class CreateCommentRequest
{
    [Required, MinLength(1), MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}

/// <summary>Search, filter, sort and pagination inputs for GET /api/tickets. All applied server-side.</summary>
public sealed class TicketQuery : PagedQuery
{
    /// <summary>Matches ticket number, title or description, case-insensitively.</summary>
    [MaxLength(200)]
    public string? Search { get; set; }

    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public int? CategoryId { get; set; }
    public int? AssignedToUserId { get; set; }
    public bool? Unassigned { get; set; }
    public SlaState? SlaState { get; set; }
}

public sealed record TicketListItemDto(
    int Id,
    string TicketNumber,
    string Title,
    string CategoryName,
    TicketStatus Status,
    TicketPriority Priority,
    string CreatedByName,
    string? AssignedToName,
    int? AssignedToUserId,
    DateTime SlaDueAt,
    SlaState SlaState,
    bool IsEscalated,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TicketDetailDto(
    int Id,
    string TicketNumber,
    string Title,
    string Description,
    int CategoryId,
    string CategoryName,
    TicketStatus Status,
    TicketPriority Priority,
    int CreatedByUserId,
    string CreatedByName,
    int? AssignedToUserId,
    string? AssignedToName,
    DateTime SlaDueAt,
    SlaState SlaState,
    double HoursUntilSlaDue,
    bool IsEscalated,
    DateTime? EscalatedAt,
    string? EscalationReason,
    string? Resolution,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TicketStatus> AllowedNextStatuses,
    IReadOnlyList<LinkedArticleDto> SuggestedArticles);

public sealed record LinkedArticleDto(
    int ArticleId,
    string Title,
    double RelevanceScore,
    ArticleLinkSource Source);

public sealed record TicketCommentDto(
    int Id,
    int AuthorUserId,
    string AuthorName,
    string Body,
    bool IsInternal,
    DateTime CreatedAt);

public sealed record TicketHistoryDto(
    int Id,
    string? ChangedByName,
    string Field,
    string? OldValue,
    string? NewValue,
    string? Note,
    DateTime CreatedAt);
