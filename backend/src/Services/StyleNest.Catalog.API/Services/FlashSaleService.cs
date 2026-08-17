/**
 * ENH-CAT-002 — Flash Sale Module: server-driven countdown + sold-out transition
 * Acceptance criteria:
 *   - GetActiveSalesAsync: returns sales with Status=Active AND EndsAt > UtcNow
 *   - GetFlashSaleItemsAsync: returns items for a sale, sorted by remaining stock (ascending)
 *   - RecordSaleAsync: increments SoldCount; marks IsSoldOut when SoldCount >= StockLimit (> 0)
 *   - GetActiveSalesAsync: Status=Ended or future Scheduled → excluded
 *   - SecondsRemaining: computed server-side from (EndsAt − UtcNow).TotalSeconds, floor 0
 *   - SoldOut items remain in the items list (visible but flagged)
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>ENH-CAT-002 — Summary of an active flash sale, including server-computed countdown.</summary>
public sealed record FlashSaleSummaryDto(
    Guid            Id,
    string          Name,
    DateTime        StartsAt,
    DateTime        EndsAt,
    int             SecondsRemaining,
    int             TotalItems);

/// <summary>ENH-CAT-002 — A single item within a flash sale.</summary>
public sealed record FlashSaleItemDto(
    Guid    ProductId,
    string  ProductName,
    decimal SalePrice,
    decimal OriginalPrice,
    int     StockLimit,
    int     SoldCount,
    bool    IsSoldOut,
    int     RemainingStock);   // 0 when StockLimit=0 (unlimited) or when sold out

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IFlashSaleService
{
    /// <summary>ENH-CAT-002 — Returns all active sales (Status=Active, EndsAt &gt; now).</summary>
    Task<List<FlashSaleSummaryDto>> GetActiveSalesAsync(CancellationToken ct = default);

    /// <summary>ENH-CAT-002 — Returns all items for a sale, sorted by remaining stock ascending.</summary>
    Task<List<FlashSaleItemDto>> GetFlashSaleItemsAsync(Guid flashSaleId, CancellationToken ct = default);

    /// <summary>
    /// ENH-CAT-002 — Increments SoldCount for the given item by <paramref name="quantity"/>.
    /// Sets IsSoldOut=true when SoldCount ≥ StockLimit (StockLimit &gt; 0).
    /// No-op when item not found.
    /// </summary>
    Task RecordSaleAsync(Guid flashSaleId, Guid productId, int quantity = 1, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class FlashSaleService(
    AppDbContext db,
    ILogger<FlashSaleService> logger) : IFlashSaleService
{
    public async Task<List<FlashSaleSummaryDto>> GetActiveSalesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var sales = await db.FlashSales
            .AsNoTracking()
            .Where(fs => fs.Status == FlashSaleStatus.Active && fs.EndsAt > now)
            .Select(fs => new FlashSaleSummaryDto(
                fs.Id,
                fs.Name,
                fs.StartsAt,
                fs.EndsAt,
                (int)Math.Max(0, (fs.EndsAt - now).TotalSeconds),
                fs.Items.Count(i => !i.IsDeleted)))
            .ToListAsync(ct);

        return sales;
    }

    public async Task<List<FlashSaleItemDto>> GetFlashSaleItemsAsync(
        Guid flashSaleId, CancellationToken ct = default)
    {
        var items = await db.FlashSaleItems
            .AsNoTracking()
            .Where(fi => fi.FlashSaleId == flashSaleId)
            .Include(fi => fi.Product)
            .Select(fi => new FlashSaleItemDto(
                fi.ProductId,
                fi.Product.Name,
                fi.SalePrice,
                fi.OriginalPrice,
                fi.StockLimit,
                fi.SoldCount,
                fi.IsSoldOut,
                fi.StockLimit > 0
                    ? Math.Max(0, fi.StockLimit - fi.SoldCount)
                    : 0))
            .OrderBy(fi => fi.IsSoldOut)                  // available first
            .ThenBy(fi => fi.RemainingStock)              // fewest remaining first (scarcity signal)
            .ToListAsync(ct);

        return items;
    }

    public async Task RecordSaleAsync(
        Guid flashSaleId, Guid productId, int quantity = 1, CancellationToken ct = default)
    {
        var item = await db.FlashSaleItems
            .FirstOrDefaultAsync(fi => fi.FlashSaleId == flashSaleId && fi.ProductId == productId, ct);

        if (item is null)
        {
            logger.LogWarning(
                "RecordSale: FlashSaleItem not found for FlashSaleId={FlashSaleId} ProductId={ProductId}",
                flashSaleId, productId);
            return;
        }

        item.SoldCount += quantity;

        // Server-driven sold-out transition
        if (item.StockLimit > 0 && item.SoldCount >= item.StockLimit)
        {
            item.IsSoldOut = true;
            logger.LogInformation(
                "{EventType} FlashSaleId={FlashSaleId} ProductId={ProductId} SoldCount={SoldCount}",
                "FLASH_SALE_SOLD_OUT", flashSaleId, productId, item.SoldCount);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "{EventType} FlashSaleId={FlashSaleId} ProductId={ProductId} Quantity={Quantity} NewSoldCount={SoldCount}",
            "FLASH_SALE_RECORDED", flashSaleId, productId, quantity, item.SoldCount);
    }
}
