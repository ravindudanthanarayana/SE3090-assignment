using System.Text.Json;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Infrastructure.Persistence;

/// <summary>
/// Writes the audit trail. Details are serialised as jsonb; callers pass small anonymous objects,
/// never whole entities, so no password hash or token can reach this table.
/// </summary>
public sealed class AuditService(IAppDbContext db, IClock clock) : IAuditService
{
    public async Task LogAsync(
        string entityType, object entityId, string action,
        ActorType actorType = ActorType.User, int? actorUserId = null,
        object? details = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId.ToString() ?? string.Empty,
            Action = action,
            ActorUserId = actorUserId,
            ActorType = actorType,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
