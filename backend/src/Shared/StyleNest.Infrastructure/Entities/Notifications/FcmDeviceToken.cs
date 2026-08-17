using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Notifications;

/// <summary>
/// ENH-NOTIF-002 — Stores an FCM device registration token per user per device.
/// Tokens are refreshed in-place (upsert by UserId + DeviceId).
/// Stale tokens (FCM returns UNREGISTERED) are soft-deleted immediately.
/// </summary>
public class FcmDeviceToken : BaseEntity<Guid>
{
    public Guid UserId { get; set; }

    /// <summary>Stable device/browser fingerprint provided by the client.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>FCM registration token — max 4096 chars per Google docs.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>"android" | "ios" | "web"</summary>
    public string Platform { get; set; } = string.Empty;
}
