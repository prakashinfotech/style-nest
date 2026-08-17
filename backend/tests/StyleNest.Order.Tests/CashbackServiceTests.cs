/**
 * ENH-PROMO-001 — StyleNest Cash Earn on Purchase (FR-PROMO / SOW §3.10)
 * Acceptance criteria:
 *   - CreditAsync credits EarnPercent % of orderTotal to wallet (round to 2 d.p.)
 *   - CreditAsync creates a wallet row when user has none yet
 *   - CreditAsync sets TransactionSource = CashbackReward
 *   - CreditAsync sets Reference = orderNumber
 *   - CreditAsync updates Wallet.Balance correctly
 *   - CreditAsync sets BalanceAfter on WalletTransaction correctly
 *   - CreditAsync is a no-op when Enabled = false
 *   - CreditAsync is a no-op when cashback rounds to zero
 *   - CreditAsync accumulates correctly on a second call (existing wallet)
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StyleNest.Infrastructure.Entities.Wallet;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class CashbackServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public CashbackServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── credit amount calculation ──────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_Credits1PercentOfOrderTotal()
    {
        var sut    = BuildSut(earnPercent: 1.0m);
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 1000m);

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(10.00m);   // 1% of 1000
    }

    [Fact]
    public async Task CreditAsync_RoundsToTwoDecimalPlaces()
    {
        var sut    = BuildSut(earnPercent: 1.5m);
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 333m);

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(5.00m);   // 1.5% of 333 = 4.995 → rounds to 5.00
    }

    // ── wallet creation ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_CreatesWalletWhenNoneExists()
    {
        var sut    = BuildSut();
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 500m);

        var wallets = await _db.Wallets.Where(w => w.UserId == userId).ToListAsync();
        wallets.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreditAsync_AccumulatesOnExistingWallet()
    {
        var sut    = BuildSut(earnPercent: 2.0m);
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 500m);  // +10
        await sut.CreditAsync(userId, "ORD-002", 1000m); // +20

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(30.00m);
    }

    // ── transaction fields ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_SetsTransactionSourceToCashbackReward()
    {
        var sut    = BuildSut();
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 1000m);

        var tx = await _db.WalletTransactions.FirstAsync();
        tx.Source.Should().Be(TransactionSource.CashbackReward);
        tx.Type.Should().Be(TransactionType.Credit);
    }

    [Fact]
    public async Task CreditAsync_SetsReferenceToOrderNumber()
    {
        var sut    = BuildSut();
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-XYZ-999", 1000m);

        var tx = await _db.WalletTransactions.FirstAsync();
        tx.Reference.Should().Be("ORD-XYZ-999");
    }

    [Fact]
    public async Task CreditAsync_SetsBalanceAfterCorrectly()
    {
        var sut    = BuildSut(earnPercent: 1.0m);
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 2000m); // +20

        var tx = await _db.WalletTransactions.FirstAsync();
        tx.BalanceAfter.Should().Be(20.00m);
    }

    // ── feature flag ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_WhenDisabled_WritesNoRows()
    {
        var sut    = BuildSut(enabled: false);
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 1000m);

        (await _db.Wallets.AnyAsync(w => w.UserId == userId)).Should().BeFalse();
        (await _db.WalletTransactions.AnyAsync()).Should().BeFalse();
    }

    // ── zero amount guard ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_ZeroOrderTotal_WritesNoRows()
    {
        var sut    = BuildSut();
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 0m);

        (await _db.Wallets.AnyAsync(w => w.UserId == userId)).Should().BeFalse();
    }

    [Fact]
    public async Task CreditAsync_TinyAmountRoundsToZero_WritesNoRows()
    {
        // 1% of 0.09 = 0.0009 → rounds to 0.00
        var sut    = BuildSut(earnPercent: 1.0m);
        var userId = Guid.NewGuid();

        await sut.CreditAsync(userId, "ORD-001", 0.09m);

        (await _db.Wallets.AnyAsync(w => w.UserId == userId)).Should().BeFalse();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private CashbackService BuildSut(decimal earnPercent = 1.0m, bool enabled = true)
    {
        var settings = Options.Create(new CashbackSettings
        {
            EarnPercent = earnPercent,
            Enabled     = enabled,
        });
        return new CashbackService(_db, settings, NullLogger<CashbackService>.Instance);
    }
}
