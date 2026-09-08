using SmartDesk.Infrastructure.Security;

namespace SmartDesk.Api.Infrastructure;

/// <summary>
/// Supplies claims from the current request so the Infrastructure layer's CurrentUser does not
/// need its own dependency on ASP.NET Core.
/// </summary>
public sealed class HttpContextAccessorAdapter(IHttpContextAccessor accessor) : IHttpContextAccessorAdapter
{
    public string? FindClaim(string claimType) =>
        accessor.HttpContext?.User?.FindFirst(claimType)?.Value;
}
