using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

/// <summary>
/// ENH-PDP-001 — Pincode delivery serviceability record.
/// Seeded with 12 representative pincode types (serviceable, non-serviceable,
/// COD-eligible, COD-blacklisted, express, standard).
/// </summary>
public class PincodeServiceability : BaseEntity<Guid>
{
    /// <summary>6-digit Indian postal code.</summary>
    public string Pincode { get; set; } = string.Empty;

    /// <summary>Whether the pincode is covered by any delivery network.</summary>
    public bool IsServiceable { get; set; }

    /// <summary>Whether Cash-On-Delivery is permitted for this pincode.</summary>
    public bool CodEligible { get; set; }

    /// <summary>Estimated transit days under standard delivery.</summary>
    public int EtaDays { get; set; }

    /// <summary>Whether express (next-day / same-day) delivery is available.</summary>
    public bool ExpressAvailable { get; set; }

    /// <summary>Minimum order value (₹) for free standard delivery at this pincode.</summary>
    public decimal FreeDeliveryThreshold { get; set; } = 499m;

    /// <summary>City / region name for display (e.g., "Mumbai").</summary>
    public string? City { get; set; }
}
