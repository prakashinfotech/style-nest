using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Orders;

/// <summary>
/// ENH-ORD-005 — Shiprocket / Delhivery AWB shipment tracking event.
/// One record per status update received from the carrier or via webhook.
/// </summary>
public class ShipmentTracking : BaseEntity<Guid>
{
    public Guid OrderId { get; set; }

    /// <summary>
    /// Carrier event code, e.g. "PICKED_UP", "IN_TRANSIT", "OUT_FOR_DELIVERY",
    /// "DELIVERED", "NDR", "RTO_INITIATED".
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Human-readable event description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Location string from carrier, e.g. "Mumbai Hub".</summary>
    public string? Location { get; set; }

    /// <summary>UTC timestamp as reported by the carrier (may differ from <see cref="BaseEntity{TKey}.CreatedAt"/>).</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>NDR reason code when EventType = "NDR", e.g. "CUSTOMER_UNAVAILABLE".</summary>
    public string? NdrReason { get; set; }

    public Order Order { get; set; } = null!;
}
