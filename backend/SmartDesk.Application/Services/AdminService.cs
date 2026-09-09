using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Services;

/// <summary>Admin-only management of users, roles, ticket categories and the audit trail.</summary>
public sealed class AdminService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher hasher,
    IAuditService audit,
    IClock clock)
{
    // ---- Users --------------------------------------------------------------------------

    public async Task<PagedResult<UserDto>> QueryUsersAsync(UserQuery query, CancellationToken ct = default)
    {
        var q = db.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.Role)) q = q.Where(u => u.Role.Name == query.Role);
        if (query.IsActive.HasValue) q = q.Where(u => u.IsActive == query.IsActive);

        q = (query.SortBy?.ToLowerInvariant(), query.Descending) switch
        {
            ("email", true) => q.OrderByDescending(u => u.Email),
            ("email", false) => q.OrderBy(u => u.Email),
            ("createdat", true) => q.OrderByDescending(u => u.CreatedAt),
            ("createdat", false) => q.OrderBy(u => u.CreatedAt),
            (_, true) => q.OrderByDescending(u => u.FullName),
            _ => q.OrderBy(u => u.FullName)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<UserDto>(items.Select(AuthService.ToDto).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("An account with this email address already exists.");

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role, ct)
            ?? throw new ValidationException($"Role '{request.Role}' does not exist. Valid roles: {string.Join(", ", RoleNames.All)}.");

        var now = clock.UtcNow;
        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Department = request.Department?.Trim(),
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        user.Role = role;

        await audit.LogAsync("User", user.Id, "UserCreated", ActorType.User, currentUser.UserId,
            new { email, role = role.Name }, ct);

        return AuthService.ToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id);

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role, ct)
            ?? throw new ValidationException($"Role '{request.Role}' does not exist.");

        // An admin must not be able to lock themselves out or demote themselves by accident.
        if (id == currentUser.UserId && (!request.IsActive || role.Name != RoleNames.Admin))
            throw new ValidationException("You cannot deactivate or demote your own administrator account.");

        user.FullName = request.FullName.Trim();
        user.Department = request.Department?.Trim();
        user.RoleId = role.Id;
        user.IsActive = request.IsActive;
        user.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        user.Role = role;

        await audit.LogAsync("User", id, "UserUpdated", ActorType.User, currentUser.UserId,
            new { role = role.Name, request.IsActive }, ct);

        return AuthService.ToDto(user);
    }

    public async Task DeactivateUserAsync(int id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id);

        if (id == currentUser.UserId)
            throw new ValidationException("You cannot deactivate your own account.");

        // Users are deactivated rather than deleted, so their tickets, comments and audit rows survive.
        user.IsActive = false;
        user.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("User", id, "UserDeactivated", ActorType.User, currentUser.UserId, null, ct);
    }

    // ---- Categories ---------------------------------------------------------------------

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => await db.TicketCategories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.DefaultSlaHours, c.IsActive, c.Tickets.Count))
            .ToListAsync(ct);

    public async Task<CategoryDto> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await db.TicketCategories.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct))
            throw new ConflictException($"A category named '{name}' already exists.");

        var now = clock.UtcNow;
        var category = new TicketCategory
        {
            Name = name,
            Description = request.Description?.Trim(),
            DefaultSlaHours = request.DefaultSlaHours,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.TicketCategories.Add(category);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("TicketCategory", category.Id, "CategoryCreated", ActorType.User, currentUser.UserId,
            new { name }, ct);

        return new CategoryDto(category.Id, category.Name, category.Description,
            category.DefaultSlaHours, category.IsActive, 0);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int id, UpsertCategoryRequest request, CancellationToken ct = default)
    {
        var category = await db.TicketCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("TicketCategory", id);

        var name = request.Name.Trim();
        if (await db.TicketCategories.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), ct))
            throw new ConflictException($"A category named '{name}' already exists.");

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.DefaultSlaHours = request.DefaultSlaHours;
        category.IsActive = request.IsActive;
        category.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("TicketCategory", id, "CategoryUpdated", ActorType.User, currentUser.UserId, null, ct);

        var count = await db.Tickets.CountAsync(t => t.CategoryId == id, ct);
        return new CategoryDto(category.Id, category.Name, category.Description,
            category.DefaultSlaHours, category.IsActive, count);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var category = await db.TicketCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("TicketCategory", id);

        // The FK is RESTRICT, so refuse clearly instead of surfacing a database error.
        if (await db.Tickets.AnyAsync(t => t.CategoryId == id, ct))
            throw new ConflictException("This category still has tickets. Deactivate it instead of deleting it.");

        db.TicketCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("TicketCategory", id, "CategoryDeleted", ActorType.User, currentUser.UserId, null, ct);
    }

    // ---- Audit trail --------------------------------------------------------------------

    public async Task<PagedResult<AuditLogDto>> QueryAuditAsync(AuditQuery query, CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsNoTracking().Include(a => a.ActorUser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.EntityId)) q = q.Where(a => a.EntityId == query.EntityId);
        if (!string.IsNullOrWhiteSpace(query.Action)) q = q.Where(a => a.Action == query.Action);
        if (query.ActorUserId.HasValue) q = q.Where(a => a.ActorUserId == query.ActorUserId);

        q = query.SortDir == "asc" ? q.OrderBy(a => a.CreatedAt) : q.OrderByDescending(a => a.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityType, a.EntityId, a.Action,
                a.ActorUser != null ? a.ActorUser.FullName : null,
                a.ActorType.ToString(), a.DetailsJson, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, query.Page, query.PageSize, total);
    }
}
