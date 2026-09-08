using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Services;

public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    IAuditService audit,
    IClock clock)
{
    /// <summary>
    /// Self-registration always creates an Employee. Staff and admin accounts are created by an Admin
    /// through /api/users, so nobody can grant themselves a privileged role at sign-up.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("An account with this email address already exists.");

        var role = await db.Roles.FirstAsync(r => r.Name == RoleNames.Employee, ct);

        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Department = request.Department?.Trim(),
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        user.Role = role;

        await audit.LogAsync("User", user.Id, "UserRegistered", ActorType.User, user.Id, new { email }, ct);

        var (token, expiresAt) = tokens.CreateToken(user);
        return new AuthResponse(token, expiresAt, ToDto(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same message whether the email is unknown or the password is wrong, so the endpoint
        // cannot be used to enumerate registered accounts.
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new ForbiddenException("Invalid email address or password.");

        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        await audit.LogAsync("User", user.Id, "UserLoggedIn", ActorType.User, user.Id, null, ct);

        var (token, expiresAt) = tokens.CreateToken(user);
        return new AuthResponse(token, expiresAt, ToDto(user));
    }

    public async Task<UserDto> GetCurrentAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Role).AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);
        return ToDto(user);
    }

    internal static UserDto ToDto(User u) =>
        new(u.Id, u.Email, u.FullName, u.Department, u.Role.Name, u.IsActive, u.CreatedAt);
}
