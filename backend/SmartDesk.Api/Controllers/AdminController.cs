using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;

namespace SmartDesk.Api.Controllers;

/// <summary>Administrative management of users and roles.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.Admin)]
[Produces("application/json")]
public sealed class UsersController(AdminService admin) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> List([FromQuery] UserQuery query, CancellationToken ct)
        => Ok(await admin.QueryUsersAsync(query, ct));

    /// <summary>Creates a staff or admin account. This is the only way a privileged role is granted.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await admin.CreateUserAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request, CancellationToken ct)
        => Ok(await admin.UpdateUserAsync(id, request, ct));

    /// <summary>Deactivates rather than deletes, so the user's tickets and audit history survive.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await admin.DeactivateUserAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Ticket category management. Categories drive both classification and SLA windows.</summary>
[ApiController]
[Route("api/categories")]
[Authorize]
[Produces("application/json")]
public sealed class CategoriesController(AdminService admin) : ControllerBase
{
    /// <summary>Readable by any authenticated user, because the create-ticket form needs it.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(CancellationToken ct)
        => Ok(await admin.GetCategoriesAsync(ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(UpsertCategoryRequest request, CancellationToken ct)
    {
        var category = await admin.CreateCategoryAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoryDto>> Update(int id, UpsertCategoryRequest request, CancellationToken ct)
        => Ok(await admin.UpdateCategoryAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await admin.DeleteCategoryAsync(id, ct);
        return NoContent();
    }
}

/// <summary>The system-wide audit trail (spec section 5: history and observability).</summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = RoleNames.ManagerOrAdmin)]
[Produces("application/json")]
public sealed class AuditLogsController(AdminService admin) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List([FromQuery] AuditQuery query, CancellationToken ct)
        => Ok(await admin.QueryAuditAsync(query, ct));
}
