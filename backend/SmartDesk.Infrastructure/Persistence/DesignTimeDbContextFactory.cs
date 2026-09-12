using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartDesk.Infrastructure.Persistence;

/// <summary>
/// Used only by "dotnet ef" at design time. Migrations are generated from the model and the provider,
/// so this does not need a reachable database - but if DATABASE_CONNECTION_STRING is set, commands
/// such as "database update" will use it.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Database=smartdesk_design;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionStringNormalizer.Normalize(connectionString))
            .Options;

        return new AppDbContext(options);
    }
}
