using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Entities;

namespace SmartDesk.Infrastructure.Security;

/// <summary>JWT settings. The signing key comes from configuration/environment and is never committed.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SmartDeskAI";
    public string Audience { get; set; } = "SmartDeskAIClients";
    public int ExpiryMinutes { get; set; } = 480;
}

/// <summary>
/// BCrypt password hashing. BCrypt generates and stores its own per-password salt and is
/// deliberately slow, which is what makes an offline attack on a leaked hash expensive.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed stored hash must fail closed, not throw a 500.
            return false;
        }
    }
}

public sealed class JwtTokenService(JwtOptions options) : ITokenService
{
    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(options.ExpiryMinutes);

        // Only identity and role go into the token. No email content, no permissions list,
        // nothing the backend would then have to trust the client about.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

/// <summary>Reads the authenticated caller out of the current HTTP request's claims.</summary>
public sealed class CurrentUser(IHttpContextAccessorAdapter accessor) : ICurrentUser
{
    public int? UserId =>
        int.TryParse(accessor.FindClaim(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => accessor.FindClaim(ClaimTypes.Email) ?? accessor.FindClaim(JwtRegisteredClaimNames.Email);

    public string? Role => accessor.FindClaim(ClaimTypes.Role);

    public bool IsAuthenticated => UserId is not null;

    public bool IsInRole(params string[] roles) =>
        Role is not null && roles.Contains(Role, StringComparer.Ordinal);
}

/// <summary>
/// Keeps Infrastructure free of a direct ASP.NET Core dependency; the API project supplies the
/// implementation that reads HttpContext.
/// </summary>
public interface IHttpContextAccessorAdapter
{
    string? FindClaim(string claimType);
}
