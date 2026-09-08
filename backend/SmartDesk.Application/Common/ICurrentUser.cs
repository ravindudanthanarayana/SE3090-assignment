namespace SmartDesk.Application.Common;

/// <summary>
/// The authenticated caller, resolved from the JWT by the API layer.
/// Services use this for resource-ownership checks that a [Authorize(Roles=...)] attribute cannot express.
/// </summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(params string[] roles);
}
