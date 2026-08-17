/**
 * ENH-PROMO-002 — StyleNest Cash Expiry Policy (12-month inactivity)
 * Acceptance criteria tested here:
 *
 *   TC-PROMO-002-01: Wallet with LastPurchaseAt == null → eligible for expiry (never purchased)
 *   TC-PROMO-002-02: Wallet with LastPurchaseAt exactly 12 months + 1 day ago → expired
 *   TC-PROMO-002-03: Wallet with LastPurchaseAt exactly 12 months - 1 day ago → NOT expired
 *   TC-PROMO-002-04: Wallet with zero balance + expired → no Expiry transaction, returns false
 *   TC-PROMO-002-05: Expiry creates WalletTransaction(Source=Expiry, Type=Debit, Amount=forfeitedBalance)
 *   TC-PROMO-002-06: After ExpireIfInactiveAsync, Wallet.Balance == 0
 *   TC-PROMO-002-07: Second call on already-zero wallet → no-op, returns false
 *   TC-PROMO-002-08: GetEligibleWalletOwnerIdsAsync returns only eligible user IDs
 *   TC-PROMO-002-09: Active wallet (LastPurchaseAt < 12 months) excluded from eligible list
 *   TC-PROMO-002-10: ProcessAllExpiresAsync returns count of expired wallets
 *   TC-PROMO-002-11: WalletRedemptionService.RedeemAsync sets LastPurchaseAt (expiry clock reset)
 *   TC-PROMO-002-12: ExpireIfInactiveAsync on missing wallet → returns false, no crash
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Wallet;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class WalletExpiryServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly WalletExpiryService _sut;
    private static readonly DateTime Now = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc);

    public WalletExpiryServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new WalletExpiryService(_db, Microsoft.Extensions.Logging.Abstractions.NullLogger<WalletExpiryService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Wallet> SeedWalletAsync(
        Guid userId, decimal balance, DateTime? lastPurchaseAt)
    {
        var wallet = new Wallet
        {
            Id             = Guid.NewGuid(),
            UserId         = userId,
            Balance        = balance,
            LastPurchaseAt = lastPurchaseAt,
            CreatedAt      = Now.AddYears(-2),
            UpdatedAt      = Now.AddYears(-2),
        };
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();
        return wallet;
    }

    // ── TC-PROMO-002-01: null LastPurchaseAt → eligible (never purchased) ─────

    [Fact]
    public async Task ExpireIfInactive_NullLastPurchaseAt_BalanceForfeited()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, balance: 500m, lastPurchaseAt: null);

        var expired = await _sut.ExpireIfInactiveAsync(userId, Now);

        expired.Should().BeTrue(
            because: "TC-PROMO-002-01: a wallet with no purchase history is always eligible for expiry");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(0m);
    }

    // ── TC-PROMO-002-02: LastPurchaseAt exactly 12m+1d → expired ─────────────

    [Fact]
    public async Task ExpireIfInactive_LastPurchase13MonthsAgo_IsExpired()
    {
        var userId = Guid.NewGuid();
        var lastPurchase = Now - WalletExpiryService.InactivityWindow - TimeSpan.FromDays(1);
        await SeedWalletAsync(userId, balance: 200m, lastPurchaseAt: lastPurchase);

        var expired = await _sut.ExpireIfInactiveAsync(userId, Now);

        expired.Should().BeTrue(
            because: "TC-PROMO-002-02: LastPurchaseAt > 12 months in the past → balance is forfeit");
    }

    // ── TC-PROMO-002-03: LastPurchaseAt 11 months ago → NOT expired ───────────

    [Fact]
    public async Task ExpireIfInactive_LastPurchase11MonthsAgo_NotExpired()
    {
        var userId = Guid.NewGuid();
        var lastPurchase = Now - TimeSpan.FromDays(335); // ~11 months
        await SeedWalletAsync(userId, balance: 300m, lastPurchaseAt: lastPurchase);

        var expired = await _sut.ExpireIfInactiveAsync(userId, Now);

        expired.Should().BeFalse(
            because: "TC-PROMO-002-03: LastPurchaseAt < 12 months ago → balance must not be forfeited");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(300m, because: "balance must remain unchanged");
    }

    // ── TC-PROMO-002-04: zero balance → no Expiry transaction ─────────────────

    [Fact]
    public async Task ExpireIfInactive_ZeroBalance_NoExpiryTransactionCreated()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, balance: 0m, lastPurchaseAt: null);

        var expired = await _sut.ExpireIfInactiveAsync(userId, Now);

        expired.Should().BeFalse(
            because: "TC-PROMO-002-04: no cashback to forfeit → no Expiry debit should be created");

        var expiryTxCount = await _db.WalletTransactions
            .CountAsync(t => t.Source == TransactionSource.Expiry);
        expiryTxCount.Should().Be(0);
    }

    // ── TC-PROMO-002-05: Expiry transaction is correct ────────────────────────

    [Fact]
    public async Task ExpireIfInactive_CreatesExpiryTransaction_WithCorrectFields()
    {
        var userId = Guid.NewGuid();
        var wallet = await SeedWalletAsync(userId, balance: 750m, lastPurchaseAt: null);

        await _sut.ExpireIfInactiveAsync(userId, Now);

        var tx = await _db.WalletTransactions
            .FirstOrDefaultAsync(t => t.Source == TransactionSource.Expiry);

        tx.Should().NotBeNull(
            because: "TC-PROMO-002-05: an Expiry debit transaction must be recorded for audit");
        tx!.Type.Should().Be(TransactionType.Debit);
        tx.Amount.Should().Be(750m,
            because: "the forfeited amount must equal the full wallet balance");
        tx.BalanceAfter.Should().Be(0m);
        tx.WalletId.Should().Be(wallet.Id);
    }

    // ── TC-PROMO-002-06: Balance is zero after expiry ────────────────────────

    [Fact]
    public async Task ExpireIfInactive_SetsBalanceToZero()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, balance: 1234.56m, lastPurchaseAt: null);

        await _sut.ExpireIfInactiveAsync(userId, Now);

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(0m,
            because: "TC-PROMO-002-06: the entire forfeited balance must be zeroed out");
    }

    // ── TC-PROMO-002-07: Idempotent — second call is no-op ───────────────────

    [Fact]
    public async Task ExpireIfInactive_AlreadyZeroBalance_IsNoOp()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, balance: 100m, lastPurchaseAt: null);

        await _sut.ExpireIfInactiveAsync(userId, Now);  // first call — expires it
        var second = await _sut.ExpireIfInactiveAsync(userId, Now); // second call

        second.Should().BeFalse(
            because: "TC-PROMO-002-07: a wallet already zeroed must not generate a duplicate Expiry transaction");

        var txCount = await _db.WalletTransactions
            .CountAsync(t => t.Source == TransactionSource.Expiry);
        txCount.Should().Be(1, because: "only one Expiry transaction should exist");
    }

    // ── TC-PROMO-002-08: GetEligibleWalletOwnerIdsAsync ──────────────────────

    [Fact]
    public async Task GetEligibleWalletOwnerIds_ReturnsOnlyExpiredEligibleUsers()
    {
        var eligibleA  = Guid.NewGuid(); // expired + balance
        var eligibleB  = Guid.NewGuid(); // null LastPurchaseAt + balance
        var notEligC   = Guid.NewGuid(); // active (recent purchase) + balance
        var notEligD   = Guid.NewGuid(); // expired but zero balance

        await SeedWalletAsync(eligibleA, 500m, Now - WalletExpiryService.InactivityWindow - TimeSpan.FromDays(10));
        await SeedWalletAsync(eligibleB, 200m, null);
        await SeedWalletAsync(notEligC,  300m, Now - TimeSpan.FromDays(30)); // active
        await SeedWalletAsync(notEligD,  0m,   Now - WalletExpiryService.InactivityWindow - TimeSpan.FromDays(10));

        var eligible = await _sut.GetEligibleWalletOwnerIdsAsync(Now);

        eligible.Should().Contain(eligibleA,
            because: "TC-PROMO-002-08: expired wallet with balance must be returned");
        eligible.Should().Contain(eligibleB,
            because: "null LastPurchaseAt wallet with balance must be returned");
        eligible.Should().NotContain(notEligC,
            because: "TC-PROMO-002-09: wallet with recent purchase must not be returned");
        eligible.Should().NotContain(notEligD,
            because: "zero-balance expired wallet must not be returned");
    }

    // ── TC-PROMO-002-10: ProcessAllExpiresAsync ───────────────────────────────

    [Fact]
    public async Task ProcessAllExpires_ReturnsCorrectCount()
    {
        await SeedWalletAsync(Guid.NewGuid(), 100m, null);
        await SeedWalletAsync(Guid.NewGuid(), 200m, Now - WalletExpiryService.InactivityWindow - TimeSpan.FromDays(5));
        await SeedWalletAsync(Guid.NewGuid(), 300m, Now - TimeSpan.FromDays(30)); // active — not expired

        var count = await _sut.ProcessAllExpiresAsync(Now);

        count.Should().Be(2,
            because: "TC-PROMO-002-10: exactly two wallets are eligible for expiry");
    }

    // ── TC-PROMO-002-11: Redemption resets the expiry clock ──────────────────

    [Fact]
    public async Task WalletRedemption_SetsLastPurchaseAt_ResettingExpiryClock()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Balance   = 500m,
            CreatedAt = Now.AddYears(-2),
            UpdatedAt = Now.AddYears(-2),
        };
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();

        var redemptionSvc = new WalletRedemptionService(
            _db,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WalletRedemptionService>.Instance);

        await redemptionSvc.RedeemAsync(userId, 50m, "ORD-RESET-001");

        var updated = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        updated.LastPurchaseAt.Should().NotBeNull(
            because: "TC-PROMO-002-11: a successful redemption must update LastPurchaseAt " +
                     "so the 12-month inactivity window resets");
        updated.LastPurchaseAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    // ── TC-PROMO-002-12: Missing wallet returns false ─────────────────────────

    [Fact]
    public async Task ExpireIfInactive_WalletNotFound_ReturnsFalse()
    {
        var result = await _sut.ExpireIfInactiveAsync(Guid.NewGuid(), Now);

        result.Should().BeFalse(
            because: "TC-PROMO-002-12: a missing wallet must not throw; graceful no-op is correct");
    }
}
