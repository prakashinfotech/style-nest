using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Notifications;

public enum OutboxStatus
{
    Pending,
    Delivered,
    Failed,
    DeadLettered
}

/// <summary>
/// ENH-NOTIF-001 — Outbox record for reliable notification delivery with
/// exponential-backoff retry (1m → 3m → 9m) then DLQ.
/// </summary>
public class NotificationOutbox : BaseEntity<Guid>
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}
