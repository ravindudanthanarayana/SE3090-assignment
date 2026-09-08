using Npgsql;

namespace SmartDesk.Infrastructure.Persistence;

/// <summary>
/// Accepts either connection-string format and returns the one Npgsql understands.
///
/// Neon (and Supabase, Railway, Heroku) hand you a URI:
///     postgresql://user:password@host/database?sslmode=require
/// Npgsql only parses key-value form:
///     Host=...;Database=...;Username=...;Password=...;SSL Mode=Require
///
/// Pasting the URI straight in fails with an unhelpful error, so we convert it here rather than
/// making every developer remember the difference.
/// </summary>
public static class ConnectionStringNormalizer
{
    public static string Normalize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("The connection string is empty.", nameof(connectionString));

        var trimmed = connectionString.Trim();

        // Already key-value form: pass it through untouched.
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        };

        var query = ParseQuery(uri.Query);

        // Neon always requires TLS. Default to Require rather than Disable, so omitting the
        // parameter cannot silently downgrade the connection.
        builder.SslMode = query.GetValueOrDefault("sslmode")?.ToLowerInvariant() switch
        {
            "disable" => SslMode.Disable,
            "allow" => SslMode.Allow,
            "prefer" => SslMode.Prefer,
            "verify-ca" => SslMode.VerifyCA,
            "verify-full" => SslMode.VerifyFull,
            _ => SslMode.Require,
        };

        // channel_binding=require is in Neon's URI but has no Npgsql equivalent; it is negotiated
        // automatically during SCRAM authentication, so it is safe to drop.

        return builder.ToString();
    }

    /// <summary>Minimal query-string parser, so this has no dependency outside Npgsql.</summary>
    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
        }

        return result;
    }
}
