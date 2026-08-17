/**
 * ENH-ORD-005 — Shiprocket / Delhivery AWB + Tracking + NDR Integration
 * Acceptance criteria:
 *   - CreateShipmentAsync assigns AWB number + carrier to order, creates PICKED_UP event
 *   - GetTrackingAsync returns all events for an order, ordered by OccurredAt
 *   - HandleNdrAsync adds an NDR event with the carrier reason code
 *   - IShippingProviderClient is mockable for unit tests (no live API calls in tests)
 *   - AWB format: carrier-prefixed (e.g. "SRT-XXXXXX" for Shiprocket)
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Order.API.Services;

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>ENH-ORD-005 — Shipping provider configuration.</summary>
public sealed class ShippingSettings
{
    public const string Section = "Shipping";

    /// <summary>"SHIPROCKET" | "DELHIVERY" | "MOCK"</summary>
    public string Provider { get; init; } = "MOCK";

    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiKey     { get; init; } = string.Empty;
}

// ── AWB provider client (mockable) ────────────────────────────────────────────

/// <summary>
/// ENH-ORD-005 — Abstracts the live carrier API call for AWB generation.
/// The <see cref="MockShippingProviderClient"/> is used in dev/test; swap in
/// a Shiprocket or Delhivery implementation for production.
/// </summary>
public interface IShippingProviderClient
{
    /// <summary>Requests an AWB number from the carrier. Returns the AWB string.</summary>
    Task<string> CreateAwbAsync(Guid orderId, string orderNumber, CancellationToken ct = default);
}

/// <summary>Dev / unit-test implementation — generates deterministic AWB numbers without network calls.</summary>
public sealed class MockShippingProviderClient(
    IOptions<ShippingSettings> options) : IShippingProviderClient
{
    public Task<string> CreateAwbAsync(Guid orderId, string orderNumber, CancellationToken ct = default)
    {
        var prefix = options.Value.Provider switch
        {
            "SHIPROCKET" => "SRT",
            "DELHIVERY"  => "DEL",
            _            => "MOCK",
        };

        // Deterministic: last 6 hex chars of orderId + orderNumber suffix
        var suffix = orderId.ToString("N")[^6..].ToUpperInvariant();
        return Task.FromResult($"{prefix}-{suffix}");
    }
}

// ── Response records ──────────────────────────────────────────────────────────

/// <summary>ENH-ORD-005 — Single shipment tracking event returned to the client.</summary>
public sealed record TrackingEventDto(
    string   EventType,
    string   Description,
    string?  Location,
    DateTime OccurredAt,
    string?  NdrReason);

/// <summary>ENH-ORD-005 — Full tracking response for an order.</summary>
public sealed record OrderTrackingResponse(
    Guid                    OrderId,
    string                  OrderNumber,
    string?                 AwbNumber,
    string?                 CarrierName,
    List<TrackingEventDto>  Events);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IShippingService
{
    /// <summary>
    /// ENH-ORD-005 — Assigns an AWB number to the order (via carrier client) and records
    /// the initial "PICKED_UP" tracking event.
    /// </summary>
    Task CreateShipmentAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>ENH-ORD-005 — Returns all tracking events for the order, newest-first.</summary>
    Task<OrderTrackingResponse?> GetTrackingAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// ENH-ORD-005 — Records an NDR (Non-Delivery Report) event for the order.
    /// Called from the carrier's NDR webhook.
    /// </summary>
    Task HandleNdrAsync(Guid orderId, string ndrReason, string? location, CancellationToken ct = default);

    /// <summary>ENH-ORD-005 — Appends an arbitrary carrier tracking event to the order.</summary>
    Task AddTrackingEventAsync(
        Guid orderId, string eventType, string description,
        string? location, DateTime? occurredAt, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class ShippingService(
    AppDbContext db,
    IShippingProviderClient providerClient,
    IOptions<ShippingSettings> options,
    ILogger<ShippingService> logger) : IShippingService
{
    public async Task CreateShipmentAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            logger.LogWarning("CreateShipment: order {OrderId} not found", orderId);
            return;
        }

        // Generate AWB via carrier client
        var awb = await providerClient.CreateAwbAsync(orderId, order.OrderNumber, ct);

        order.AwbNumber   = awb;
        order.CarrierName = options.Value.Provider;

        db.ShipmentTrackings.Add(new ShipmentTracking
        {
            Id          = Guid.NewGuid(),
            OrderId     = orderId,
            EventType   = "PICKED_UP",
            Description = $"Shipment picked up by {options.Value.Provider}",
            OccurredAt  = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "{EventType} OrderId={OrderId} AwbNumber={AwbNumber} Carrier={Carrier}",
            "SHIPMENT_CREATED", orderId, awb, options.Value.Provider);
    }

    public async Task<OrderTrackingResponse?> GetTrackingAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null) return null;

        var events = await db.ShipmentTrackings
            .AsNoTracking()
            .Where(t => t.OrderId == orderId)
            .OrderBy(t => t.OccurredAt)
            .Select(t => new TrackingEventDto(
                t.EventType, t.Description, t.Location, t.OccurredAt, t.NdrReason))
            .ToListAsync(ct);

        return new OrderTrackingResponse(
            OrderId:     orderId,
            OrderNumber: order.OrderNumber,
            AwbNumber:   order.AwbNumber,
            CarrierName: order.CarrierName,
            Events:      events);
    }

    public async Task HandleNdrAsync(
        Guid orderId, string ndrReason, string? location, CancellationToken ct = default)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            logger.LogWarning("HandleNdr: order {OrderId} not found", orderId);
            return;
        }

        db.ShipmentTrackings.Add(new ShipmentTracking
        {
            Id          = Guid.NewGuid(),
            OrderId     = orderId,
            EventType   = "NDR",
            Description = $"Delivery attempt failed: {ndrReason}",
            Location    = location,
            OccurredAt  = DateTime.UtcNow,
            NdrReason   = ndrReason,
        });

        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "{EventType} OrderId={OrderId} Reason={Reason}",
            "NDR_RECEIVED", orderId, ndrReason);
    }

    public async Task AddTrackingEventAsync(
        Guid orderId, string eventType, string description,
        string? location, DateTime? occurredAt, CancellationToken ct = default)
    {
        db.ShipmentTrackings.Add(new ShipmentTracking
        {
            Id          = Guid.NewGuid(),
            OrderId     = orderId,
            EventType   = eventType,
            Description = description,
            Location    = location,
            OccurredAt  = occurredAt ?? DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }
}
