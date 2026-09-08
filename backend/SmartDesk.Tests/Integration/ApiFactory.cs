using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SmartDesk.Tests.Integration;

/// <summary>
/// Boots the real API in memory against the throwaway PostgreSQL database.
///
/// The only thing substituted is the language model: no AI_API_KEY is supplied, so
/// DependencyInjection falls back to ScriptedLlmClient. Every other component - controllers,
/// middleware, JWT validation, services, business rules, orchestrator, tools, EF Core - is
/// the production one.
/// </summary>
public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_CONNECTION_STRING"] = connectionString,
            ["JWT_SECRET"] = "integration-test-signing-key-only-never-used-in-production-0123456789",
            // Left empty on purpose: this is what selects the scripted model and the null email provider.
            ["AI_API_KEY"] = "",
            ["NOTIFICATION_API_KEY"] = "",
            ["RunMigrationsOnStartup"] = "false",
            ["Agents:RetryBackoffMs"] = "1"
        }));

        return base.CreateHost(builder);
    }
}
