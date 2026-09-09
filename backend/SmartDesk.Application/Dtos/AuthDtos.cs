using System.ComponentModel.DataAnnotations;

namespace SmartDesk.Application.Dtos;

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed record UserDto(
    int Id,
    string Email,
    string FullName,
    string? Department,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
