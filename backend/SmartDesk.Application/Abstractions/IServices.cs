using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}

/// <summary>Writes the audit trail. Called by services, never by an agent directly.</summary>
public interface IAuditService
{
    Task LogAsync(
        string entityType,
        object entityId,
        string action,
        ActorType actorType = ActorType.User,
        int? actorUserId = null,
        object? details = null,
        CancellationToken ct = default);
}

/// <summary>
/// Third-party notification integration (spec section 11). Implementations must never throw into
/// the caller's business transaction - a failed send is recorded and swallowed.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(int userId, int? ticketId, string subject, string body, CancellationToken ct = default);
}

/// <summary>The transport used by the notification service. Swappable for tests and offline demos.</summary>
public interface IEmailProvider
{
    string Name { get; }
    Task<EmailSendResult> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}

public sealed record EmailSendResult(bool Success, string? MessageId, string? Error);

/// <summary>
/// The only way any agent reaches a language model. Implementations must request JSON-only output.
/// ScriptedLlmClient makes every agent evaluation test deterministic and offline.
/// </summary>
public interface ILlmClient
{
    string ProviderName { get; }
    Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default);
}

/// <summary>
/// Applies an approved high-impact action to the database inside a single transaction.
/// Shared by ApprovalService (the real path) and the ExecuteApprovedAction tool (defence in depth).
/// </summary>
public interface IApprovalActionExecutor
{
    Task ExecuteAsync(AiApproval approval, CancellationToken ct = default);
}
