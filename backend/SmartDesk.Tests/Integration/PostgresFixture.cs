using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Infrastructure.Security;

namespace SmartDesk.Tests.Integration;

/// <summary>
/// Creates a throwaway PostgreSQL database per test run from the same EF migrations used in
/// production, then drops it. Nothing here ever touches the deployed Neon database.
///
/// The server is taken from TEST_DATABASE_CONNECTION_STRING. In CI that is the postgres service
/// container; locally it is a docker container (see README).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private string _adminConnectionString = string.Empty;
    public string ConnectionString { get; private set; } = string.Empty;
    public string DatabaseName { get; } = $"smartdesk_test_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        _adminConnectionString =
            Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            try
            {
                await admin.OpenAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "The PostgreSQL integration tests need a reachable PostgreSQL server. " +
                    "Start one with:\n" +
                    "  docker run -d --name smartdesk-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine\n" +
                    "or set TEST_DATABASE_CONNECTION_STRING to an existing server.\n" +
                    $"Underlying error: {ex.Message}", ex);
            }

            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{DatabaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = DatabaseName };
        ConnectionString = builder.ToString();

        // Applying the real migrations is itself part of what these tests verify.
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        await Seed(db);
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task Seed(AppDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SEED_PASSWORD"] = "Password123!" })
            .Build();

        var seeder = new DbSeeder(db, new BCryptPasswordHasher(), config, NullLogger<DbSeeder>.Instance);
        await seeder.SeedAsync();
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
