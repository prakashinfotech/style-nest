/**
 * ENH-PAY-007 — Wallet StyleNest Cash Redemption with pessimistic SELECT…WITH (UPDLOCK, ROWLOCK)
 * Acceptance criteria:
 *   - RedeemAsync acquires a row-level pessimistic write lock before deducting balance
 *   - Insufficient balance → InsufficientWalletBalanceException (amount, available)
 *   - Same orderNumber → idempotent no-op (guards duplicate webhook / double-tap)
 *   - WalletTransaction created: Type=Debit, Source=Redemption, Reference=orderNumber
 *   - GetBalanceAsync returns 0 when no wallet row exists
 *   - Structured log events: STYLENEST_CASH_REDEEMED, STYLENEST_CASH_REDEMPTION_IDEMPOTENT
 */

using System.Data;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Wallet;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Order.API.Services;

// ── Exception ─────────────────────────────────────────────────────────────────

/// <summary>
/// ENH-PAY-007 — Thrown when the user's StyleNest Cash balance is insufficient
/// for the requested redemption amount.
/// </summary>
public sealed class InsufficientWalletBalanceException(decimal requested, decimal available)
    : Exception($"Insufficient StyleNest Cash balance: requested ₹{requested:N2}, available ₹{available:N2}")
{
    public decimal Requested  { get; } = requested;
    public decimal Available  { get; } = available;
}

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IWalletRedemptionService
{
    /// <summary>
    /// ENH-PAY-007 — Debits StyleNest Cash from the user's wallet inside a pessimistic-locked
    /// SQL Server transaction (SELECT … WITH (UPDLOCK, ROWLOCK)) to prevent double-spend.
    /// Throws <see cref="InsufficientWalletBalanceException"/> when balance is too low.
    /// Same <paramref name="orderNumber"/> is a no-op (idempotent).
    /// </summary>
    Task RedeemAsync(Guid userId, decimal amount, string orderNumber, CancellationToken ct = default);

    /// <summary>Returns the current StyleNest Cash balance. Returns 0 when no wallet exists.</summary>
    Task<decimal> GetBalanceAsync(Guid userId, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// ENH-PAY-007 — Wallet StyleNest Cash debit with pessimistic UPDLOCK/ROWLOCK concurrency guard.
/// On SQL Server: issues SELECT * FROM [wallet].[Wallets] WITH (UPDLOCK, ROWLOCK) so that
/// concurrent checkout sessions cannot read the same stale balance simultaneously.
/// On InMemory (unit tests): falls through to a normal EF query — locking is not exercised.
/// </summary>
public sealed class WalletRedemptionService(
    AppDbContext db,
    ILogger<WalletRedemptionService> logger) : IWalletRedemptionService
{
    public async Task RedeemAsync(
        Guid userId, decimal amount, string orderNumber, CancellationToken ct = default)
    {
        if (amount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Redemption amount must be positive.");

        // SQL Server: wrap in a read-committed transaction so the UPDLOCK/ROWLOCK hint
        // is held for the full duration of the debit.
        // InMemory (unit tests): BeginTransactionAsync is a no-op; run core logic directly.
        if (db.Database.IsSqlServer())
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await ExecuteRedeemCoreAsync(userId, amount, orderNumber, ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        else
        {
            await ExecuteRedeemCoreAsync(userId, amount, orderNumber, ct);
        }
    }

    private async Task ExecuteRedeemCoreAsync(
        Guid userId, decimal amount, string orderNumber, CancellationToken ct)
    {
        // ── Idempotency guard ──────────────────────────────────────────────
        // If an active Redemption transaction already references this order, return early.
        var alreadyRedeemed = await db.WalletTransactions
            .AnyAsync(t => t.Reference == orderNumber
                        && t.Source    == TransactionSource.Redemption, ct);

        if (alreadyRedeemed)
        {
            logger.LogInformation(
                "{EventType} UserId={UserId} OrderNumber={OrderNumber}",
                "STYLENEST_CASH_REDEMPTION_IDEMPOTENT", userId, orderNumber);
            return;
        }

        // ── Pessimistic lock ───────────────────────────────────────────────
        // On SQL Server this acquires an UPDLOCK + ROWLOCK on the wallet row for the
        // duration of the transaction, preventing concurrent reads of a stale balance.
        var wallet = await FetchWithLockAsync(userId, ct);

        if (wallet is null)
            throw new InvalidOperationException($"Wallet not found for user {userId}.");

        if (wallet.Balance < amount)
            throw new InsufficientWalletBalanceException(amount, wallet.Balance);

        // ── Debit ──────────────────────────────────────────────────────────
        wallet.Balance -= amount;
        // ENH-PROMO-002: reset the 12-month inactivity clock on every purchase debit
        wallet.LastPurchaseAt = DateTime.UtcNow;

        db.WalletTransactions.Add(new WalletTransaction
        {
            Id           = Guid.NewGuid(),
            WalletId     = wallet.Id,
            Amount       = amount,
            Type         = TransactionType.Debit,
            Source       = TransactionSource.Redemption,
            Description  = $"StyleNest Cash redeemed on order {orderNumber}",
            Reference    = orderNumber,
            BalanceAfter = wallet.Balance,
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "{EventType} UserId={UserId} OrderNumber={OrderNumber} Amount={Amount} RemainingBalance={Balance}",
            "STYLENEST_CASH_REDEEMED", userId, orderNumber, amount, wallet.Balance);
    }

    public async Task<decimal> GetBalanceAsync(Guid userId, CancellationToken ct = default)
    {
        var wallet = await db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        return wallet?.Balance ?? 0m;
    }

    // ── Private: provider-aware lock fetch ────────────────────────────────────

    private async Task<Wallet?> FetchWithLockAsync(Guid userId, CancellationToken ct)
    {
        if (db.Database.IsSqlServer())
        {
            // SQL Server: pessimistic write lock prevents concurrent balance reads
            // within the same transaction window.
            return await db.Wallets
                .FromSqlInterpolated(
                    $"SELECT * FROM [wallet].[Wallets] WITH (UPDLOCK, ROWLOCK) WHERE UserId = {userId} AND IsDeleted = 0")
                .FirstOrDefaultAsync(ct);
        }

        // InMemory provider (unit tests) — row locking is not supported; fallback to EF query.
        // Global soft-delete filter is applied automatically.
        return await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }
}
