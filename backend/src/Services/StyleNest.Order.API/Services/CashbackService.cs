using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StyleNest.Infrastructure.Entities.Wallet;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Order.API.Services;

/// <summary>ENH-PROMO-001 — configurable StyleNest Cash earn rate (appsettings StyleNestCash section).</summary>
public sealed class CashbackSettings
{
    public const string Section = "StyleNestCash";

    /// <summary>Percentage of order total credited as StyleNest Cash (e.g. 1.5 = 1.5%).</summary>
    public decimal EarnPercent { get; init; } = 1.0m;

    /// <summary>Feature flag — set false to disable cashback without code change.</summary>
    public bool Enabled { get; init; } = true;
}

public interface ICashbackService
{
    /// <summary>
    /// ENH-PROMO-001 — Credits StyleNest Cash to the user's wallet after a successful order.
    /// Amount = orderTotal × EarnPercent / 100, rounded to 2 d.p.
    /// No-op when Enabled=false or amount rounds to zero.
    /// Never throws — logs and swallows errors so order placement is never blocked.
    /// </summary>
    Task CreditAsync(Guid userId, string orderNumber, decimal orderTotal, CancellationToken ct = default);
}

/// <summary>
/// ENH-PROMO-001 — Writes a WalletTransaction (Source=CashbackReward) and updates Wallet.Balance.
/// Creates the wallet row if the user has none yet.
/// </summary>
public sealed class CashbackService(
    AppDbContext db,
    IOptions<CashbackSettings> options,
    ILogger<CashbackService> logger) : ICashbackService
{
    public async Task CreditAsync(Guid userId, string orderNumber, decimal orderTotal, CancellationToken ct = default)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogDebug("StyleNest Cash disabled — skipping cashback for order {OrderNumber}", orderNumber);
            return;
        }

        var cashback = Math.Round(orderTotal * settings.EarnPercent / 100m, 2, MidpointRounding.AwayFromZero);
        if (cashback <= 0m)
        {
            logger.LogDebug("Cashback amount is zero for order {OrderNumber} — skipping", orderNumber);
            return;
        }

        try
        {
            // Get or create wallet
            var wallet = await db.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId, ct);

            if (wallet is null)
            {
                wallet = new Wallet { Id = Guid.NewGuid(), UserId = userId, Balance = 0m, Currency = "INR" };
                db.Wallets.Add(wallet);
                await db.SaveChangesAsync(ct);   // persist so the FK on WalletTransaction resolves
            }

            wallet.Balance += cashback;

            db.WalletTransactions.Add(new WalletTransaction
            {
                Id           = Guid.NewGuid(),
                WalletId     = wallet.Id,
                Amount       = cashback,
                Type         = TransactionType.Credit,
                Source       = TransactionSource.CashbackReward,
                Description  = $"StyleNest Cash earned on order {orderNumber}",
                Reference    = orderNumber,
                BalanceAfter = wallet.Balance,
            });

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "{EventType} UserId={UserId} OrderNumber={OrderNumber} Cashback={Cashback} EarnPercent={EarnPercent}",
                "STYLENEST_CASH_EARNED", userId, orderNumber, cashback, settings.EarnPercent);
        }
        catch (Exception ex)
        {
            // Cashback failure must never roll back the order
            logger.LogError(ex,
                "StyleNest Cash credit failed for UserId={UserId} OrderNumber={OrderNumber}",
                userId, orderNumber);
        }
    }
}
