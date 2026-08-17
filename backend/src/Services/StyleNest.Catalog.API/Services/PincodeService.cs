/**
 * ENH-PDP-001 — Pincode Delivery Estimate
 * Acceptance criteria:
 *   - Returns serviceability, COD eligibility, ETA, express availability, free-delivery threshold
 *   - Response ≤ 1s (FR-PDP-003) — single indexed lookup on Pincode column
 *   - Unknown / degraded: returns conservative cached defaults (Serviceable=true, EtaDays=5)
 *   - Non-serviceable pincode → HTTP 200 with { serviceable: false } (not 404)
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── Response record ───────────────────────────────────────────────────────────

/// <summary>ENH-PDP-001 — Delivery estimate for a pincode.</summary>
public sealed record DeliveryEstimateResponse(
    bool    Serviceable,
    bool    CodEligible,
    int     EtaDays,
    bool    ExpressAvailable,
    decimal FreeDeliveryThreshold,
    string? City = null);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IPincodeService
{
    /// <summary>
    /// ENH-PDP-001 — Returns the delivery estimate for the given pincode.
    /// Never throws; returns degraded defaults on unknown or error.
    /// </summary>
    Task<DeliveryEstimateResponse> GetEstimateAsync(string pincode, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// ENH-PDP-001 — Looks up pincode serviceability from <see cref="AppDbContext"/>.
/// Unknown pincodes return degraded-default response (assume serviceable, 5-day ETA).
/// All exceptions are swallowed; degraded defaults are returned on any DB error.
/// </summary>
public sealed class PincodeService(
    AppDbContext db,
    ILogger<PincodeService> logger) : IPincodeService
{
    // Degraded defaults: shown when DB is unavailable or pincode not found
    internal static readonly DeliveryEstimateResponse DegradedDefaults = new(
        Serviceable:           true,
        CodEligible:           true,
        EtaDays:               5,
        ExpressAvailable:      false,
        FreeDeliveryThreshold: 499m,
        City:                  null);

    public async Task<DeliveryEstimateResponse> GetEstimateAsync(string pincode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pincode))
            return DegradedDefaults;

        try
        {
            var record = await db.PincodeServiceabilities
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Pincode == pincode, ct);

            if (record is null)
            {
                logger.LogDebug("Pincode {Pincode} not found — returning degraded defaults", pincode);
                return DegradedDefaults;
            }

            return new DeliveryEstimateResponse(
                Serviceable:           record.IsServiceable,
                CodEligible:           record.IsServiceable && record.CodEligible,
                EtaDays:               record.IsServiceable ? record.EtaDays : 0,
                ExpressAvailable:      record.IsServiceable && record.ExpressAvailable,
                FreeDeliveryThreshold: record.FreeDeliveryThreshold,
                City:                  record.City);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PincodeService: DB error for pincode {Pincode} — returning degraded defaults", pincode);
            return DegradedDefaults;
        }
    }
}
