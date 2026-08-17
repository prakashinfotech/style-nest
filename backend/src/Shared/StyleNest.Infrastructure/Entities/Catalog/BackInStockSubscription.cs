/**
 * ENH-PDP-006 — Back-in-Stock Notification.
 *
 * A shopper subscribes to a (ProductId, VariantId?) pair.
 * When stock is replenished (Stock > 0 after being 0), a batch notifier
 * reads un-notified subscriptions and dispatches email + SMS notifications,
 * then marks NotifiedAt.
 *
 * Uniqueness: one active subscription per (UserId, ProductId, VariantId?) —
 * enforced via a unique index in AppDbContext.
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class BackInStockSubscription : BaseEntity<Guid>
{
    public Guid    UserId    { get; set; }
    public Guid    ProductId { get; set; }

    /// <summary>Optional — subscribe to a specific variant (size/colour).</summary>
    public Guid?   VariantId { get; set; }

    /// <summary>Email address to notify (captured at subscribe time).</summary>
    public string  Email     { get; set; } = string.Empty;

    /// <summary>Optional mobile number (E.164, e.g. +919876543210).</summary>
    public string? Phone     { get; set; }

    /// <summary>Set when the notification has been dispatched; null = pending.</summary>
    public DateTime? NotifiedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
