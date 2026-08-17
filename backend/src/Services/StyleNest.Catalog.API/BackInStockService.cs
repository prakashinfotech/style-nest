/**
 * ENH-PDP-006 — Back-in-Stock Notification service.
 *
 * Workflow (FR-PDP-012):
 *   1. Shopper opens OOS product page → clicks "Notify Me".
 *   2. POST /api/v1/products/{id}/back-in-stock with email (+ optional phone + variantId).
 *   3. Subscription is stored in BackInStockSubscriptions.
 *   4. When stock is replenished, an admin or batch job calls
 *      NotifySubscribersAsync(productId) which:
 *        a. Reads un-notified subscriptions for the product.
 *        b. For each — dispatches a NotificationOutbox entry (email + SMS if phone present).
 *        c. Stamps NotifiedAt = UtcNow.
 *   5. User may unsubscribe via DELETE /api/v1/products/{id}/back-in-stock.
 *
 * Duplicate subscribe → idempotent: if an active subscription exists for
 * (UserId, ProductId, VariantId) it is returned as-is (no error).
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Notifications;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public sealed record SubscribeBackInStockRequest(
    string  Email,
    string? Phone     = null,
    Guid?   VariantId = null);

public sealed record BackInStockSubscriptionDto(
    Guid      Id,
    Guid      UserId,
    Guid      ProductId,
    Guid?     VariantId,
    string    Email,
    string?   Phone,
    DateTime? NotifiedAt,
    DateTime  CreatedAt);

public sealed record NotifyResult(int SubscribersNotified, Guid ProductId);

// ─── Interface ───────────────────────────────────────────────────────────────

public interface IBackInStockService
{
    /// <summary>Subscribe a user to back-in-stock notification. Idempotent.</summary>
    Task<BackInStockSubscriptionDto> SubscribeAsync(
        Guid productId, Guid userId, SubscribeBackInStockRequest req, CancellationToken ct = default);

    /// <summary>Unsubscribe a user from notifications for a product.</summary>
    Task UnsubscribeAsync(Guid productId, Guid userId, Guid? variantId, CancellationToken ct = default);

    /// <summary>Returns the active subscription for a user+product, or null.</summary>
    Task<BackInStockSubscriptionDto?> GetSubscriptionAsync(
        Guid productId, Guid userId, Guid? variantId, CancellationToken ct = default);

    /// <summary>
    /// Dispatches notifications to all un-notified subscribers for <paramref name="productId"/>.
    /// Called by a batch job or admin endpoint when stock is replenished.
    /// </summary>
    Task<NotifyResult> NotifySubscribersAsync(Guid productId, CancellationToken ct = default);
}

// ─── Implementation ──────────────────────────────────────────────────────────

public sealed class BackInStockService(AppDbContext db) : IBackInStockService
{
    /// <inheritdoc />
    public async Task<BackInStockSubscriptionDto> SubscribeAsync(
        Guid productId, Guid userId, SubscribeBackInStockRequest req, CancellationToken ct = default)
    {
        // Idempotency: return existing active subscription if present
        var existing = await db.BackInStockSubscriptions
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.ProductId == productId &&
                s.VariantId == req.VariantId, ct);

        if (existing is not null)
            return MapDto(existing);

        var sub = new BackInStockSubscription
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            ProductId = productId,
            VariantId = req.VariantId,
            Email     = req.Email.Trim().ToLowerInvariant(),
            Phone     = req.Phone?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.BackInStockSubscriptions.Add(sub);
        await db.SaveChangesAsync(ct);
        return MapDto(sub);
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(
        Guid productId, Guid userId, Guid? variantId, CancellationToken ct = default)
    {
        var sub = await db.BackInStockSubscriptions
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.ProductId == productId &&
                s.VariantId == variantId, ct);

        if (sub is null) return; // already gone — idempotent

        sub.IsDeleted = true;
        sub.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<BackInStockSubscriptionDto?> GetSubscriptionAsync(
        Guid productId, Guid userId, Guid? variantId, CancellationToken ct = default)
    {
        var sub = await db.BackInStockSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.ProductId == productId &&
                s.VariantId == variantId, ct);

        return sub is null ? null : MapDto(sub);
    }

    /// <inheritdoc />
    public async Task<NotifyResult> NotifySubscribersAsync(
        Guid productId, CancellationToken ct = default)
    {
        var pending = await db.BackInStockSubscriptions
            .Where(s => s.ProductId == productId && s.NotifiedAt == null)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return new NotifyResult(0, productId);

        var now = DateTime.UtcNow;

        foreach (var sub in pending)
        {
            // Enqueue email notification via outbox pattern (ENH-NOTIF-001 outbox)
            db.NotificationOutbox.Add(new NotificationOutbox
            {
                Id           = Guid.NewGuid(),
                UserId       = sub.UserId,
                Type         = "BackInStock.Email",
                Subject      = "Your product is back in stock!",
                Body         = BuildEmailBody(sub.ProductId, sub.VariantId),
                Status       = OutboxStatus.Pending,
                NextAttemptAt = now,
                CreatedAt    = now,
                UpdatedAt    = now,
            });

            // Enqueue SMS notification if phone provided
            if (!string.IsNullOrWhiteSpace(sub.Phone))
            {
                db.NotificationOutbox.Add(new NotificationOutbox
                {
                    Id           = Guid.NewGuid(),
                    UserId       = sub.UserId,
                    Type         = "BackInStock.SMS",
                    Subject      = "Back in stock",
                    Body         = "Great news! A product you wanted is back in stock. Shop now at stylenest.com",
                    Status       = OutboxStatus.Pending,
                    NextAttemptAt = now,
                    CreatedAt    = now,
                    UpdatedAt    = now,
                });
            }

            sub.NotifiedAt = now;
            sub.UpdatedAt  = now;
        }

        await db.SaveChangesAsync(ct);
        return new NotifyResult(pending.Count, productId);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static BackInStockSubscriptionDto MapDto(BackInStockSubscription s) =>
        new(s.Id, s.UserId, s.ProductId, s.VariantId, s.Email, s.Phone, s.NotifiedAt, s.CreatedAt);

    private static string BuildEmailBody(Guid productId, Guid? variantId)
    {
        var variant = variantId.HasValue ? $" (variant {variantId})" : string.Empty;
        return $"The product{variant} you were waiting for is now back in stock. " +
               $"Visit https://stylenest.com/products/{productId} to shop before it sells out again.";
    }
}
