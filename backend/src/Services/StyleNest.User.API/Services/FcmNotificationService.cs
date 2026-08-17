/**
 * ENH-NOTIF-002 — FCM Push Notifications
 * Acceptance criteria:
 *   - Order status change → FCM push with correct orderId and newStatus in payload
 *   - FCM device token stored per user/device; token refresh handled (old token replaced)
 *   - Stale token (FCM returns UNREGISTERED/404) → token soft-deleted, not retried
 *   - Delivery receipt stored in NotificationLogs
 *   - Structured log: FCM_PUSH_SENT / FCM_PUSH_FAILED / FCM_TOKEN_STALE
 */

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StyleNest.Infrastructure.Entities.Notifications;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.User.API.Services;

// ── Configuration ─────────────────────────────────────────────────────────────

/// <summary>ENH-NOTIF-002 — FCM HTTP v1 project settings.</summary>
public sealed class FcmSettings
{
    public const string Section = "Fcm";

    /// <summary>Firebase project identifier, e.g. "stylenest-prod".</summary>
    public string ProjectId { get; init; } = "stylenest-demo";

    /// <summary>
    /// Bearer token for FCM HTTP v1 API.
    /// In production use Google Application Default Credentials (ADC) to obtain
    /// a short-lived OAuth2 token scoped to firebase.messaging.
    /// In dev/test set this to any non-empty string; the mock handler intercepts it.
    /// </summary>
    public string BearerToken { get; init; } = string.Empty;
}

// ── Abstractions ──────────────────────────────────────────────────────────────

public interface IFcmNotificationService
{
    /// <summary>
    /// ENH-NOTIF-002 — Registers or refreshes an FCM device token for the given user.
    /// If a token already exists for (userId, deviceId) it is updated in-place.
    /// If the existing record was soft-deleted (e.g. previously stale) it is restored.
    /// </summary>
    Task RegisterTokenAsync(
        Guid userId, string deviceId, string token, string platform,
        CancellationToken ct = default);

    /// <summary>
    /// ENH-NOTIF-002 — Sends an FCM push notification for an order status update
    /// to all active registered devices for the given user.
    /// Stale tokens (FCM HTTP 404 UNREGISTERED) are soft-deleted immediately.
    /// A <see cref="NotificationLog"/> is written for each push attempt.
    /// </summary>
    Task SendOrderUpdateAsync(
        Guid userId, Guid orderId, string orderNumber, string newStatus,
        CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// ENH-NOTIF-002 — FCM HTTP v1 notification service.
/// Uses a named <see cref="IHttpClientFactory"/> client "fcm" to call
/// https://fcm.googleapis.com/v1/projects/{projectId}/messages:send.
/// </summary>
public sealed class FcmNotificationService(
    AppDbContext db,
    IHttpClientFactory httpFactory,
    IOptions<FcmSettings> options,
    ILogger<FcmNotificationService> logger) : IFcmNotificationService
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // ── token registration ────────────────────────────────────────────────────

    public async Task RegisterTokenAsync(
        Guid userId, string deviceId, string token, string platform,
        CancellationToken ct = default)
    {
        // Bypass soft-delete filter to find even stale records (so we can restore them)
        var existing = await db.FcmDeviceTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId, ct);

        if (existing is not null)
        {
            existing.Token     = token;
            existing.Platform  = platform;
            existing.IsDeleted = false;          // restore if previously stale-deleted
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.FcmDeviceTokens.Add(new FcmDeviceToken
            {
                Id       = Guid.NewGuid(),
                UserId   = userId,
                DeviceId = deviceId,
                Token    = token,
                Platform = platform,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    // ── order push ────────────────────────────────────────────────────────────

    public async Task SendOrderUpdateAsync(
        Guid userId, Guid orderId, string orderNumber, string newStatus,
        CancellationToken ct = default)
    {
        var tokens = await db.FcmDeviceTokens
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);

        if (tokens.Count == 0) return;

        var cfg    = options.Value;
        var client = httpFactory.CreateClient("fcm");

        foreach (var deviceToken in tokens)
        {
            var delivered = await SendFcmPushAsync(
                client, cfg, deviceToken, orderId, orderNumber, newStatus, ct);

            // Write delivery receipt
            db.NotificationLogs.Add(new NotificationLog
            {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Type      = delivered ? "FCM_ORDER_UPDATE" : "FCM_ORDER_UPDATE_FAILED",
                Subject   = $"Order {newStatus}",
                Message   = $"{{\"orderId\":\"{orderId}\",\"orderNumber\":\"{orderNumber}\",\"newStatus\":\"{newStatus}\"}}",
                ActionUrl = $"/orders/{orderId}",
            });
        }

        await db.SaveChangesAsync(ct);
    }

    // ── private helpers ───────────────────────────────────────────────────────

    /// <returns><c>true</c> when FCM acknowledged the message successfully.</returns>
    private async Task<bool> SendFcmPushAsync(
        HttpClient client,
        FcmSettings cfg,
        FcmDeviceToken deviceToken,
        Guid orderId, string orderNumber, string newStatus,
        CancellationToken ct)
    {
        var body = new
        {
            message = new
            {
                token        = deviceToken.Token,
                notification = new
                {
                    title = "Order Update",
                    body  = $"Your order #{orderNumber} is now {newStatus}",
                },
                data = new Dictionary<string, string>
                {
                    ["orderId"]     = orderId.ToString(),
                    ["orderNumber"] = orderNumber,
                    ["newStatus"]   = newStatus,
                },
            },
        };

        var url     = $"https://fcm.googleapis.com/v1/projects/{cfg.ProjectId}/messages:send";
        var json    = JsonSerializer.Serialize(body, _jsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        if (!string.IsNullOrEmpty(cfg.BearerToken))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.BearerToken);

        HttpResponseMessage? response = null;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "{EventType} DeviceId={DeviceId} OrderId={OrderId} Reason=HttpException",
                "FCM_PUSH_FAILED", deviceToken.DeviceId, orderId);
            return false;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // FCM UNREGISTERED — token is stale; remove it so we don't retry
            deviceToken.IsDeleted = true;
            deviceToken.UpdatedAt = DateTime.UtcNow;
            logger.LogWarning(
                "{EventType} DeviceId={DeviceId} UserId={UserId} Reason=UnregisteredToken",
                "FCM_TOKEN_STALE", deviceToken.DeviceId, deviceToken.UserId);
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "{EventType} DeviceId={DeviceId} StatusCode={StatusCode} Body={Body}",
                "FCM_PUSH_FAILED", deviceToken.DeviceId, (int)response.StatusCode, err);
            return false;
        }

        logger.LogInformation(
            "{EventType} DeviceId={DeviceId} OrderId={OrderId} NewStatus={NewStatus}",
            "FCM_PUSH_SENT", deviceToken.DeviceId, orderId, newStatus);
        return true;
    }
}
