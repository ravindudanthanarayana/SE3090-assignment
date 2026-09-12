using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>"resend" for the live provider, "null" to log locally without sending.</summary>
    public string Provider { get; set; } = "null";

    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "SmartDesk AI <onboarding@resend.dev>";
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Optional. When set, every notification is delivered to this address instead of the real
    /// recipient, with the intended recipient noted in the body.
    ///
    /// Resend's free tier only permits sending to the account owner's own verified address until a
    /// domain is verified, and the seeded demo users have fictional @smartdesk.local addresses.
    /// This makes the third-party integration genuinely demonstrable without inventing real
    /// mailboxes, and without ever emailing a person who did not ask for it.
    /// Leave empty in production.
    /// </summary>
    public string RedirectAllTo { get; set; } = string.Empty;
}

/// <summary>
/// Third-party integration (spec section 11): Resend transactional email.
///
/// Business purpose - a help desk only works if people find out that something happened. We notify on
/// assignment, on escalation and on status change so support agents and requesters do not have to
/// poll the UI.
///
/// The API key lives only in the backend, so no client ever sees it. Timeouts, retries with backoff,
/// and rate limits are handled here; a send failure is recorded but never breaks the business
/// operation that triggered it.
/// </summary>
public sealed class ResendEmailProvider(
    HttpClient http,
    NotificationOptions options,
    ILogger<ResendEmailProvider> logger) : IEmailProvider
{
    public string Name => "resend";

    public async Task<EmailSendResult> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return new EmailSendResult(false, null, "No notification API key is configured.");

        var payload = new
        {
            from = options.FromAddress,
            to = new[] { toEmail },
            subject,
            // Only the ticket number, title and status go out - never the description or any other
            // personal content (spec section 11: minimise data shared with the service).
            text = body
        };

        for (var attempt = 0; attempt <= options.MaxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
                {
                    Content = JsonContent.Create(payload)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

                using var response = await http.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                    var id = json.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    return new EmailSendResult(true, id, null);
                }

                // 429 carries Retry-After; honour it rather than hammering the provider.
                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < options.MaxRetries)
                {
                    var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(delay, ct);
                    continue;
                }

                // 4xx other than 429 is our mistake and will not succeed on retry.
                if ((int)response.StatusCode < 500)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    return new EmailSendResult(false, null, $"{(int)response.StatusCode}: {Truncate(error)}");
                }

                if (attempt >= options.MaxRetries)
                    return new EmailSendResult(false, null, $"Provider returned {(int)response.StatusCode}.");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                if (attempt >= options.MaxRetries)
                    return new EmailSendResult(false, null, "The notification provider timed out.");
            }
            catch (HttpRequestException ex)
            {
                if (attempt >= options.MaxRetries)
                    return new EmailSendResult(false, null, $"Provider unreachable: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)), ct);
        }

        return new EmailSendResult(false, null, "Exhausted all retry attempts.");
    }

    private static string Truncate(string s) => s.Length <= 200 ? s : s[..200];
}

/// <summary>
/// Offline provider. Writes the same Notifications rows and logs the message, so the whole
/// notification flow stays demonstrable with no external account and no network. Used by all tests.
/// </summary>
public sealed class NullEmailProvider(ILogger<NullEmailProvider> logger) : IEmailProvider
{
    public string Name => "null";

    public Task<EmailSendResult> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("[NullEmailProvider] To {To}: {Subject}", toEmail, subject);
        return Task.FromResult(new EmailSendResult(true, $"null-{Guid.NewGuid():N}", null));
    }
}

/// <summary>
/// Records every notification and delegates delivery to the configured provider.
/// It never throws: a notification failure must not roll back the ticket change that caused it.
/// </summary>
public sealed class NotificationService(
    IAppDbContext db,
    IEmailProvider provider,
    NotificationOptions options,
    IClock clock,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(int userId, int? ticketId, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.IsActive })
                .FirstOrDefaultAsync(ct);

            if (user is null || !user.IsActive)
            {
                logger.LogInformation("Skipping notification for inactive or missing user {UserId}", userId);
                return;
            }

            var notification = new Notification
            {
                UserId = userId,
                TicketId = ticketId,
                Channel = "Email",
                Recipient = user.Email,
                Subject = subject,
                Body = body,
                Provider = provider.Name,
                Status = NotificationStatus.Pending,
                CreatedAt = clock.UtcNow
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct);

            // The redirect is applied at send time only. The Notifications row still records the
            // real intended recipient, so the audit trail stays truthful.
            var deliverTo = string.IsNullOrWhiteSpace(options.RedirectAllTo)
                ? user.Email
                : options.RedirectAllTo;

            var deliveredBody = deliverTo == user.Email
                ? body
                : $"[Development redirect — this notification was addressed to {user.Email}]\n\n{body}";

            var result = await provider.SendAsync(deliverTo, subject, deliveredBody, ct);
            notification.AttemptCount++;

            if (result.Success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.ProviderMessageId = result.MessageId;
                notification.SentAt = clock.UtcNow;
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = result.Error;
                logger.LogWarning("Notification {Id} failed: {Error}", notification.Id, result.Error);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Deliberately swallowed. Assigning a ticket must succeed even if email is completely down.
            logger.LogError(ex, "Notification for user {UserId} could not be recorded", userId);
        }
    }
}
