/**
 * ENH-PROMO-003 — Flash Sale Price Lock: server-driven, race-condition-safe
 * Acceptance criteria tested here:
 *   - TryLockPriceAsync: returns locked SalePrice for valid active sale + available stock
 *   - TryLockPriceAsync: returns null when sale is Scheduled (not yet started)
 *   - TryLockPriceAsync: returns null when sale is Ended
 *   - TryLockPriceAsync: returns null when Active sale has expired (EndsAt in past)
 *   - TryLockPriceAsync: returns null when item is already sold out
 *   - TryLockPriceAsync: returns null when requested quantity exceeds remaining stock
 *   - TryLockPriceAsync: unlimited stock (StockLimit=0) always succeeds when sale active
 *   - SoldCount is incremented after successful lock
 *   - IsSoldOut is set when this lock exhausts the stock
 *   - Savings = OriginalPrice − SalePrice in result
 *   - Non-positive quantity → throws ArgumentOutOfRangeException
 *   - Unknown FlashSaleId → returns null (sale check fails)
 *   - Unknown ProductId → returns null (item check fails)
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class FlashSalePriceLockServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public FlashSalePriceLockServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private FlashSalePriceLockService BuildSut() =>
        new(_db, NullLogger<FlashSalePriceLockService>.Instance);

    private async Task<(FlashSale sale, FlashSaleItem item, Product product)> SeedAsync(
        FlashSaleStatus status   = FlashSaleStatus.Active,
        int  stockLimit          = 10,
        int  soldCount           = 0,
        bool isSoldOut           = false,
        double endHoursFromNow   = 2.0,
        decimal salePrice        = 799m,
        decimal originalPrice    = 999m)
    {
        var product = new Product
        {
            Id        = Guid.NewGuid(),
            Name      = "Test Product",
            Slug      = $"tp-{Guid.NewGuid():N}",
            BasePrice = originalPrice,
            CategoryId = Guid.NewGuid(),
            BrandId    = Guid.NewGuid(),
        };
        _db.Products.Add(product);

        var sale = new FlashSale
        {
            Id       = Guid.NewGuid(),
            Name     = "Flash Sale",
            StartsAt = DateTime.UtcNow.AddHours(-1),
            EndsAt   = DateTime.UtcNow.AddHours(endHoursFromNow),
            Status   = status,
        };
        _db.FlashSales.Add(sale);

        var item = new FlashSaleItem
        {
            Id            = Guid.NewGuid(),
            FlashSaleId   = sale.Id,
            ProductId     = product.Id,
            SalePrice     = salePrice,
            OriginalPrice = originalPrice,
            StockLimit    = stockLimit,
            SoldCount     = soldCount,
            IsSoldOut     = isSoldOut,
        };
        _db.FlashSaleItems.Add(item);

        await _db.SaveChangesAsync();
        return (sale, item, product);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TryLock_ValidActiveSale_ReturnsLockedPrice()
    {
        var (sale, _, product) = await SeedAsync(salePrice: 700m, originalPrice: 1000m);

        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id);

        result.Should().NotBeNull();
        result!.SalePrice.Should().Be(700m);
        result.OriginalPrice.Should().Be(1000m);
    }

    [Fact]
    public async Task TryLock_SavingsComputedCorrectly()
    {
        var (sale, _, product) = await SeedAsync(salePrice: 799m, originalPrice: 999m);

        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id);

        result!.Savings.Should().Be(200m);
    }

    [Fact]
    public async Task TryLock_IncrementsSoldCount()
    {
        var (sale, item, product) = await SeedAsync(stockLimit: 10, soldCount: 2);

        await BuildSut().TryLockPriceAsync(sale.Id, product.Id, quantity: 3);

        _db.Entry(item).Reload();
        item.SoldCount.Should().Be(5);
    }

    [Fact]
    public async Task TryLock_ExhaustsStock_MarksItemSoldOut()
    {
        var (sale, item, product) = await SeedAsync(stockLimit: 5, soldCount: 4);

        await BuildSut().TryLockPriceAsync(sale.Id, product.Id, quantity: 1);

        _db.Entry(item).Reload();
        item.IsSoldOut.Should().BeTrue("last unit was just reserved");
    }

    [Fact]
    public async Task TryLock_UnlimitedStock_AlwaysSucceeds()
    {
        var (sale, item, product) = await SeedAsync(stockLimit: 0, soldCount: 9999);

        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id, quantity: 50);

        result.Should().NotBeNull("StockLimit=0 means unlimited");
        _db.Entry(item).Reload();
        item.IsSoldOut.Should().BeFalse();
    }

    // ── Returns null cases ────────────────────────────────────────────────────

    [Fact]
    public async Task TryLock_SaleIsScheduled_ReturnsNull()
    {
        var (sale, _, product) = await SeedAsync(status: FlashSaleStatus.Scheduled);

        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryLock_SaleIsEnded_ReturnsNull()
    {
        var (sale, _, product) = await SeedAsync(status: FlashSaleStatus.Ended);

        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryLock_SaleActiveButExpired_ReturnsNull()
    {
        var (sale, _, product) = await SeedAsync(
            status: FlashSaleStatus.Active, endHoursFromNow: -1.0);

        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryLock_ItemSoldOut_ReturnsNull()
    {
        var (sale, _, product) = await SeedAsync(isSoldOut: true, soldCount: 10, stockLimit: 10);

        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryLock_InsufficientStock_ReturnsNull()
    {
        var (sale, _, product) = await SeedAsync(stockLimit: 3, soldCount: 2);

        // Only 1 unit left but requesting 2
        var result = await BuildSut().TryLockPriceAsync(sale.Id, product.Id, quantity: 2);

        result.Should().BeNull("only 1 unit remaining");
    }

    [Fact]
    public async Task TryLock_InsufficientStock_DoesNotIncrementSoldCount()
    {
        var (sale, item, product) = await SeedAsync(stockLimit: 3, soldCount: 2);

        await BuildSut().TryLockPriceAsync(sale.Id, product.Id, quantity: 2);

        _db.Entry(item).Reload();
        item.SoldCount.Should().Be(2, "no stock was reserved");
    }

    [Fact]
    public async Task TryLock_UnknownFlashSaleId_ReturnsNull()
    {
        var (_, _, product) = await SeedAsync();

        var result = await BuildSut().TryLockPriceAsync(Guid.NewGuid(), product.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryLock_UnknownProductId_ReturnsNull()
    {
        var (sale, _, _) = await SeedAsync();

        var result = await BuildSut().TryLockPriceAsync(sale.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── Invalid argument ──────────────────────────────────────────────────────

    [Fact]
    public async Task TryLock_ZeroQuantity_ThrowsArgumentOutOfRange()
    {
        var (sale, _, product) = await SeedAsync();

        var act = async () => await BuildSut().TryLockPriceAsync(sale.Id, product.Id, quantity: 0);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
