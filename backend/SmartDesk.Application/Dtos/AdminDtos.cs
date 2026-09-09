using System.ComponentModel.DataAnnotations;
using SmartDesk.Application.Common;

namespace SmartDesk.Application.Dtos;

public sealed class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required, MaxLength(30)]
    public string Role { get; set; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required, MaxLength(30)]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class UserQuery : PagedQuery
{
    [MaxLength(200)]
    public string? Search { get; set; }

    [MaxLength(30)]
    public string? Role { get; set; }

    public bool? IsActive { get; set; }
}

public sealed class UpsertCategoryRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(1, 720)]
    public int DefaultSlaHours { get; set; } = 24;

    public bool IsActive { get; set; } = true;
}

public sealed record CategoryDto(
    int Id,
    string Name,
    string? Description,
    int DefaultSlaHours,
    bool IsActive,
    int TicketCount);

public sealed class AuditQuery : PagedQuery
{
    [MaxLength(60)]
    public string? EntityType { get; set; }

    [MaxLength(60)]
    public string? EntityId { get; set; }

    [MaxLength(60)]
    public string? Action { get; set; }

    public int? ActorUserId { get; set; }
}

public sealed record AuditLogDto(
    long Id,
    string EntityType,
    string EntityId,
    string Action,
    string? ActorName,
    string ActorType,
    string? DetailsJson,
    DateTime CreatedAt);
