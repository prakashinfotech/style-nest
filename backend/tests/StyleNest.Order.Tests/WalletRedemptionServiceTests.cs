/**
 * ENH-PAY-007 — Wallet StyleNest Cash Redemption pessimistic lock (SELECT…WITH (UPDLOCK, ROWLOCK))
 * Acceptance criteria tested here:
 *   - RedeemAsync deducts correct amount from wallet balance
 *   - RedeemAsync creates WalletTransaction (Debit, Redemption, correct Reference)
 *   - RedeemAsync throws InsufficientWalletBalanceException when balance < amount
 *   - RedeemAsync throws InvalidOperationException when no wallet exists for user
 *   - RedeemAsync is idempotent — same orderNumber is a no-op (no double-deduct)
 *   - RedeemAsync throws ArgumentOutOfRangeException for non-positive amount
 *   - GetBalanceAsync returns current balance
 *   - GetBalanceAsync returns 0 when no wallet exists
 *   - Multiple sequential redemptions reduce balance cumulatively
 *   - InsufficientWalletBalanceException carries Requested + Available properties
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Infrastructure.Entities.Wallet;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class WalletRedemptionServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public WalletRedemptionServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private WalletRedemptionService BuildSut() =>
        new(_db, NullLogger<WalletRedemptionService>.Instance);

    private async Task<Wallet> SeedWalletAsync(Guid userId, decimal balance)
    {
        var wallet = new Wallet
        {
            Id       = Guid.NewGuid(),
            UserId   = userId,
            Balance  = balance,
            Currency = "INR",
        };
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();
        return wallet;
    }

    // ── RedeemAsync — happy path ───────────────────────────────────────────────

    [Fact]
    public async Task Redeem_DeductsCorrectAmountFromBalance()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 500m);

        await BuildSut().RedeemAsync(userId, 200m, "TC-1001");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(300m);
    }

    [Fact]
    public async Task Redeem_CreatesDebitTransactionWithCorrectFields()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 1000m);

        await BuildSut().RedeemAsync(userId, 350m, "TC-2002");

        var tx = await _db.WalletTransactions
            .IgnoreQueryFilters()
            .SingleAsync(t => t.Reference == "TC-2002");

        tx.Amount.Should().Be(350m);
        tx.Type.Should().Be(TransactionType.Debit);
        tx.Source.Should().Be(TransactionSource.Redemption);
        tx.BalanceAfter.Should().Be(650m);
    }

    [Fact]
    public async Task Redeem_ExactBalance_ReducesToZero()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 100m);

        await BuildSut().RedeemAsync(userId, 100m, "TC-3003");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(0m);
    }

    // ── RedeemAsync — insufficient balance ───────────────────────────────────

    [Fact]
    public async Task Redeem_InsufficientBalance_ThrowsException()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 50m);

        var act = async () => await BuildSut().RedeemAsync(userId, 200m, "TC-4004");

        await act.Should().ThrowAsync<InsufficientWalletBalanceException>();
    }

    [Fact]
    public async Task Redeem_InsufficientBalance_ExceptionCarriesAmounts()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 75m);

        try
        {
            await BuildSut().RedeemAsync(userId, 300m, "TC-5005");
            Assert.Fail("Expected InsufficientWalletBalanceException");
        }
        catch (InsufficientWalletBalanceException ex)
        {
            ex.Requested.Should().Be(300m);
            ex.Available.Should().Be(75m);
        }
    }

    [Fact]
    public async Task Redeem_InsufficientBalance_DoesNotAlterBalance()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 100m);

        try { await BuildSut().RedeemAsync(userId, 999m, "TC-6006"); }
        catch (InsufficientWalletBalanceException) { /* expected */ }

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(100m, "balance must not change on failed redemption");
    }

    // ── RedeemAsync — wallet not found ────────────────────────────────────────

    [Fact]
    public async Task Redeem_NoWallet_ThrowsInvalidOperation()
    {
        var act = async () => await BuildSut().RedeemAsync(Guid.NewGuid(), 100m, "TC-7007");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Wallet not found*");
    }

    // ── RedeemAsync — idempotency ─────────────────────────────────────────────

    [Fact]
    public async Task Redeem_SameOrderNumber_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 500m);

        var sut = BuildSut();
        await sut.RedeemAsync(userId, 100m, "TC-8008");
        await sut.RedeemAsync(userId, 100m, "TC-8008");   // duplicate call

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(400m, "second call must be a no-op");

        var count = await _db.WalletTransactions
            .CountAsync(t => t.Reference == "TC-8008");
        count.Should().Be(1, "only one transaction row should exist");
    }

    // ── RedeemAsync — invalid argument ────────────────────────────────────────

    [Fact]
    public async Task Redeem_ZeroAmount_ThrowsArgumentOutOfRange()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 100m);

        var act = async () => await BuildSut().RedeemAsync(userId, 0m, "TC-9009");

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Redeem_NegativeAmount_ThrowsArgumentOutOfRange()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 100m);

        var act = async () => await BuildSut().RedeemAsync(userId, -50m, "TC-9010");

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ── RedeemAsync — cumulative deductions ───────────────────────────────────

    [Fact]
    public async Task Redeem_MultipleOrders_ReduceBalanceCumulatively()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 1000m);

        var sut = BuildSut();
        await sut.RedeemAsync(userId, 200m, "TC-A001");
        await sut.RedeemAsync(userId, 300m, "TC-A002");
        await sut.RedeemAsync(userId, 100m, "TC-A003");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(400m);
    }

    // ── GetBalanceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetBalance_ReturnsCurrentBalance()
    {
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 750m);

        var balance = await BuildSut().GetBalanceAsync(userId);

        balance.Should().Be(750m);
    }

    [Fact]
    public async Task GetBalance_NoWallet_ReturnsZero()
    {
        var balance = await BuildSut().GetBalanceAsync(Guid.NewGuid());

        balance.Should().Be(0m);
    }
}
