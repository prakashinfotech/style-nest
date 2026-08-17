/**
 * ENH-PROMO-002 — StyleNest Cash Expiry Policy (12-month inactivity)
 *
 * Policy (SOW §3.10 / FR-PROMO):
 *   • If a user has not made ANY purchase debit on their wallet in the last
 *     12 calendar months, their entire StyleNest Cash balance is forfeited.
 *   • An "Expiry" debit WalletTransaction is written so the history is auditable.
 *   • The expiry clock resets on every Redemption / OrderPayment debit.
 *   • A batch method is provided for the scheduled job to process all eligible wallets.
 *
 * Acceptance criteria (TC-PROMO-002-*):
 *   TC-PROMO-002-01: Wallet with LastPurchaseAt null for > 12 months → expired
 *   TC-PROMO-002-02: Wallet with LastPurchaseAt exactly 12 months ago → expired
 *   TC-PROMO-002-03: Wallet with LastPurchaseAt < 12 months ago → NOT expired
 *   TC-PROMO-002-04: Wallet with zero balance → no Expiry transaction created
 *   TC-PROMO-002-05: Expiry creates WalletTransaction(Source=Expiry, Type=Debit)
 *   TC-PROMO-002-06: After expiry Balance == 0
 *   TC-PROMO-002-07: Idempotent — second call on already-zero wallet is a no-op
 *   TC-PROMO-002-08: GetEligibleForExpiryAsync returns only wallets with balance > 0
 *                     AND LastPurchaseAt outside the 12-month window
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Wallet;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Order.API.Services;

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IWalletExpiryService
{
    /// <summary>
    /// ENH-PROMO-002 — Expires StyleNest Cash for a single user if they have been inactive
    /// for more than 12 months. Returns <c>true</c> if the balance was forfeited.
    /// </summary>
    Task<bool> ExpireIfInactiveAsync(Guid userId, DateTime now, CancellationToken ct = default);

    /// <summary>
    /// ENH-PROMO-002 — Returns all wallet IDs eligible for expiry at the given <paramref name="now"/>.
    /// Eligible = Balance > 0 AND (LastPurchaseAt is null OR LastPurchaseAt &lt; now - 12 months).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetEligibleWalletOwnerIdsAsync(DateTime now, CancellationToken ct = default);

    /// <summary>
    /// ENH-PROMO-002 — Batch: expire all wallets returned by <see cref="GetEligibleWalletOwnerIdsAsync"/>.
    /// Returns the count of wallets whose balance was forfeited.
    /// </summary>
    Task<int> ProcessAllExpiresAsync(DateTime now, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>ENH-PROMO-002 — 12-month inactivity forfeiture for StyleNest Cash balances.</summary>
public sealed class WalletExpiryService(
    AppDbContext db,
    ILogger<WalletExpiryService> logger) : IWalletExpiryService
{
    /// <summary>
    /// The inactivity window after which a StyleNest Cash balance is forfeited.
    /// SOW §3.10 specifies 12 calendar months.
    /// </summary>
    public static readonly TimeSpan InactivityWindow = TimeSpan.FromDays(365);

    public async Task<bool> ExpireIfInactiveAsync(
        Guid userId, DateTime now, CancellationToken ct = default)
    {
        var wallet = await db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet is null)
            return false;

        if (!IsEligibleForExpiry(wallet, now))
            return false;

        // Balance is already 0 — idempotent no-op, no transaction needed
        if (wallet.Balance <= 0m)
            return false;

        var forfeitedAmount = wallet.Balance;

        db.WalletTransactions.Add(new WalletTransaction
        {
            Id           = Guid.NewGuid(),
            WalletId     = wallet.Id,
            Amount       = forfeitedAmount,
            Type         = TransactionType.Debit,
            Source       = TransactionSource.Expiry,
            Description  = "StyleNest Cash expired due to 12-month inactivity",
            Reference    = $"EXPIRY-{now:yyyyMMdd}",
            BalanceAfter = 0m,
            CreatedAt    = now,
            UpdatedAt    = now,
        });

        wallet.Balance = 0m;
        wallet.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "{EventType} UserId={UserId} ForfeitedAmount={Amount} LastPurchaseAt={LastPurchaseAt}",
            "STYLENEST_CASH_EXPIRED", userId, forfeitedAmount, wallet.LastPurchaseAt);

        return true;
    }

    public async Task<IReadOnlyList<Guid>> GetEligibleWalletOwnerIdsAsync(
        DateTime now, CancellationToken ct = default)
    {
        var cutoff = now - InactivityWindow;

        return await db.Wallets
            .Where(w => w.Balance > 0 &&
                        (w.LastPurchaseAt == null || w.LastPurchaseAt < cutoff))
            .Select(w => w.UserId)
            .ToListAsync(ct);
    }

    public async Task<int> ProcessAllExpiresAsync(
        DateTime now, CancellationToken ct = default)
    {
        var userIds = await GetEligibleWalletOwnerIdsAsync(now, ct);
        int count = 0;

        foreach (var userId in userIds)
        {
            if (await ExpireIfInactiveAsync(userId, now, ct))
                count++;
        }

        logger.LogInformation(
            "{EventType} ProcessedAt={Now} ExpiredWallets={Count}",
            "STYLENEST_CASH_EXPIRY_BATCH_COMPLETE", now, count);

        return count;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static bool IsEligibleForExpiry(Wallet wallet, DateTime now)
    {
        var cutoff = now - InactivityWindow;

        // null LastPurchaseAt means the user has never made a purchase — balance is stale
        return wallet.LastPurchaseAt is null || wallet.LastPurchaseAt.Value < cutoff;
    }
}
