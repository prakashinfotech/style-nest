/**
 * ENH-PROMO-003 — Flash Sale Price Lock: server-driven, race-condition-safe
 * Acceptance criteria:
 *   - TryLockPriceAsync: returns locked SalePrice + reserves stock atomically
 *   - Sale not active (Scheduled/Ended/expired) → returns null (no stock reserved)
 *   - Item sold out → returns null
 *   - Requested quantity > remaining stock → returns null
 *   - Unlimited stock (StockLimit=0) → always succeeds when sale is active
 *   - SoldCount incremented after successful lock
 *   - IsSoldOut set when SoldCount reaches StockLimit after this lock
 *   - On SQL Server: runs inside ReadCommitted transaction for race-condition safety
 *   - On InMemory (tests): runs core logic directly (transaction not supported)
 *   - Structured log events: FLASH_SALE_PRICE_LOCKED
 */

using System.Data;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── Result ────────────────────────────────────────────────────────────────────

/// <summary>ENH-PROMO-003 — Result returned when a flash sale price lock succeeds.</summary>
public sealed record FlashSalePriceLockResult(
    Guid    FlashSaleItemId,
    Guid    FlashSaleId,
    Guid    ProductId,
    decimal SalePrice,
    decimal OriginalPrice,
    decimal Savings);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IFlashSalePriceLockService
{
    /// <summary>
    /// ENH-PROMO-003 — Atomically validates the flash sale is active, the item is available,
    /// and there is sufficient stock for <paramref name="quantity"/> units.
    /// When all checks pass the stock is reserved (SoldCount incremented) and the locked
    /// <see cref="FlashSalePriceLockResult"/> is returned; otherwise returns <see langword="null"/>.
    /// On SQL Server this runs inside a ReadCommitted transaction for race-condition safety.
    /// </summary>
    Task<FlashSalePriceLockResult?> TryLockPriceAsync(
        Guid flashSaleId, Guid productId, int quantity = 1, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class FlashSalePriceLockService(
    AppDbContext db,
    ILogger<FlashSalePriceLockService> logger) : IFlashSalePriceLockService
{
    public async Task<FlashSalePriceLockResult?> TryLockPriceAsync(
        Guid flashSaleId, Guid productId, int quantity = 1, CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        // On SQL Server: wrap in ReadCommitted transaction so the row read + update is atomic.
        // On InMemory (unit tests): BeginTransactionAsync throws; run core logic directly.
        if (db.Database.IsSqlServer())
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var result = await ExecuteLockCoreAsync(flashSaleId, productId, quantity, ct);
                await tx.CommitAsync(ct);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        return await ExecuteLockCoreAsync(flashSaleId, productId, quantity, ct);
    }

    // ── Core logic (provider-agnostic) ────────────────────────────────────────

    private async Task<FlashSalePriceLockResult?> ExecuteLockCoreAsync(
        Guid flashSaleId, Guid productId, int quantity, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // ── 1. Validate flash sale is active ───────────────────────────────────
        var saleExists = await db.FlashSales
            .AnyAsync(fs => fs.Id == flashSaleId
                         && fs.Status == FlashSaleStatus.Active
                         && fs.EndsAt  > now, ct);

        if (!saleExists)
        {
            logger.LogInformation(
                "PriceLock: FlashSaleId={FlashSaleId} is not active or has ended",
                flashSaleId);
            return null;
        }

        // ── 2. Load item with tracking (so EF tracks SoldCount update) ────────
        var item = await db.FlashSaleItems
            .FirstOrDefaultAsync(
                fi => fi.FlashSaleId == flashSaleId
                   && fi.ProductId   == productId
                   && !fi.IsSoldOut, ct);

        if (item is null)
        {
            logger.LogInformation(
                "PriceLock: FlashSaleItem ProductId={ProductId} not found or sold out for FlashSaleId={FlashSaleId}",
                productId, flashSaleId);
            return null;
        }

        // ── 3. Verify sufficient stock ─────────────────────────────────────────
        // StockLimit = 0 means unlimited — skip the check.
        if (item.StockLimit > 0 && item.SoldCount + quantity > item.StockLimit)
        {
            logger.LogInformation(
                "PriceLock: Insufficient stock for ProductId={ProductId} FlashSaleId={FlashSaleId} — requested={Qty} remaining={Remaining}",
                productId, flashSaleId, quantity, item.StockLimit - item.SoldCount);
            return null;
        }

        // ── 4. Reserve stock ───────────────────────────────────────────────────
        item.SoldCount += quantity;

        if (item.StockLimit > 0 && item.SoldCount >= item.StockLimit)
        {
            item.IsSoldOut = true;

            logger.LogInformation(
                "{EventType} FlashSaleId={FlashSaleId} ProductId={ProductId}",
                "FLASH_SALE_SOLD_OUT_LOCK", flashSaleId, productId);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "{EventType} FlashSaleId={FlashSaleId} ProductId={ProductId} Quantity={Qty} SalePrice={Price}",
            "FLASH_SALE_PRICE_LOCKED", flashSaleId, productId, quantity, item.SalePrice);

        return new FlashSalePriceLockResult(
            FlashSaleItemId: item.Id,
            FlashSaleId:     flashSaleId,
            ProductId:       productId,
            SalePrice:       item.SalePrice,
            OriginalPrice:   item.OriginalPrice,
            Savings:         item.OriginalPrice - item.SalePrice);
    }
}
