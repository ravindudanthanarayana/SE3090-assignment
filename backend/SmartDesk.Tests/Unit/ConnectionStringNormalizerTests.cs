using Npgsql;
using SmartDesk.Infrastructure.Persistence;

namespace SmartDesk.Tests.Unit;

/// <summary>
/// Neon, Supabase, Railway and Heroku all hand out a postgresql:// URI, but Npgsql only parses
/// key-value form. Getting this wrong produces a confusing runtime error, so it is worth testing.
/// </summary>
public class ConnectionStringNormalizerTests
{
    private const string NeonUri =
        "postgresql://neon_user:secret_pw@ep-example-pooler.us-east-2.aws.neon.tech/neondb?sslmode=require&channel_binding=require";

    [Fact]
    public void Converts_a_neon_uri_into_a_connection_string_npgsql_can_parse()
    {
        var result = ConnectionStringNormalizer.Normalize(NeonUri);
        var builder = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("ep-example-pooler.us-east-2.aws.neon.tech", builder.Host);
        Assert.Equal("neondb", builder.Database);
        Assert.Equal("neon_user", builder.Username);
        Assert.Equal("secret_pw", builder.Password);
        Assert.Equal(5432, builder.Port);
    }

    [Fact]
    public void Honours_sslmode_from_the_uri()
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionStringNormalizer.Normalize(NeonUri));
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Defaults_to_requiring_tls_when_the_uri_omits_sslmode()
    {
        // Omitting the parameter must never silently downgrade to an unencrypted connection.
        var result = ConnectionStringNormalizer.Normalize("postgresql://u:p@host.example.com/db");
        var builder = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Ignores_channel_binding_which_has_no_npgsql_equivalent()
    {
        // It is negotiated during SCRAM authentication, so dropping it is safe - but the
        // conversion must not choke on the parameter being present.
        var result = ConnectionStringNormalizer.Normalize(NeonUri);
        Assert.DoesNotContain("channel_binding", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reads_an_explicit_port()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            ConnectionStringNormalizer.Normalize("postgresql://u:p@localhost:6543/db?sslmode=disable"));

        Assert.Equal(6543, builder.Port);
        Assert.Equal(SslMode.Disable, builder.SslMode);
    }

    [Fact]
    public void Decodes_percent_encoded_credentials()
    {
        // A password containing @ or / must be percent-encoded in a URI, and must come back intact.
        var result = ConnectionStringNormalizer.Normalize(
            "postgresql://my%40user:p%40ss%2Fword@host.example.com/db");
        var builder = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("my@user", builder.Username);
        Assert.Equal("p@ss/word", builder.Password);
    }

    [Theory]
    [InlineData("Host=localhost;Database=smartdesk;Username=postgres;Password=postgres")]
    [InlineData("Host=neon.tech;Database=db;Username=u;Password=p;SSL Mode=Require")]
    public void Passes_an_existing_key_value_connection_string_through_untouched(string keyValue)
    {
        Assert.Equal(keyValue, ConnectionStringNormalizer.Normalize(keyValue));
    }

    [Fact]
    public void Accepts_the_postgres_scheme_as_well_as_postgresql()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            ConnectionStringNormalizer.Normalize("postgres://u:p@host.example.com/db"));

        Assert.Equal("host.example.com", builder.Host);
    }

    [Fact]
    public void Trims_surrounding_whitespace_from_a_pasted_value()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            ConnectionStringNormalizer.Normalize("  postgresql://u:p@host.example.com/db  "));

        Assert.Equal("host.example.com", builder.Host);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_an_empty_connection_string_with_a_clear_error(string value)
    {
        Assert.Throws<ArgumentException>(() => ConnectionStringNormalizer.Normalize(value));
    }
}
