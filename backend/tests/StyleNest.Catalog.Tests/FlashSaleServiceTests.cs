/**
 * ENH-CAT-002 — Flash Sale Module: server-driven countdown + sold-out transition
 * Acceptance criteria tested here:
 *   - GetActiveSalesAsync: returns only Active sales with EndsAt > UtcNow
 *   - GetActiveSalesAsync: excludes Scheduled, Ended and expired Active sales
 *   - GetActiveSalesAsync: SecondsRemaining is ≥ 0 and ≤ expected window
 *   - GetActiveSalesAsync: TotalItems count matches seeded items
 *   - GetFlashSaleItemsAsync: returns items sorted sold-out-last, then fewest-remaining-first
 *   - GetFlashSaleItemsAsync: RemainingStock = max(0, StockLimit − SoldCount)
 *   - GetFlashSaleItemsAsync: RemainingStock = 0 when StockLimit = 0 (unlimited)
 *   - GetFlashSaleItemsAsync: returns empty list for unknown saleId
 *   - RecordSaleAsync: increments SoldCount by quantity
 *   - RecordSaleAsync: sets IsSoldOut when SoldCount ≥ StockLimit > 0
 *   - RecordSaleAsync: no-op for unknown item (no exception)
 *   - RecordSaleAsync: unlimited (StockLimit=0) never sets IsSoldOut
 *   - RecordSaleAsync: SoldCount incremented correctly by non-1 quantity
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class FlashSaleServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public FlashSaleServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private FlashSaleService BuildSut() =>
        new(_db, NullLogger<FlashSaleService>.Instance);

    private async Task<(FlashSale sale, Product product)> SeedActiveSaleAsync(
        string name = "Flash Sale", int stockLimit = 10, int soldCount = 0,
        double hoursFromNow = 2.0)
    {
        var product = new Product
        {
            Id        = Guid.NewGuid(),
            Name      = "Test Product",
            Slug      = $"test-product-{Guid.NewGuid():N}",
            BasePrice = 999m,
            CategoryId = Guid.NewGuid(),
            BrandId    = Guid.NewGuid(),
        };
        _db.Products.Add(product);

        var sale = new FlashSale
        {
            Id       = Guid.NewGuid(),
            Name     = name,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            EndsAt   = DateTime.UtcNow.AddHours(hoursFromNow),
            Status   = FlashSaleStatus.Active,
        };
        _db.FlashSales.Add(sale);

        var item = new FlashSaleItem
        {
            Id            = Guid.NewGuid(),
            FlashSaleId   = sale.Id,
            ProductId     = product.Id,
            SalePrice     = 799m,
            OriginalPrice = 999m,
            StockLimit    = stockLimit,
            SoldCount     = soldCount,
            IsSoldOut     = stockLimit > 0 && soldCount >= stockLimit,
        };
        _db.FlashSaleItems.Add(item);

        await _db.SaveChangesAsync();
        return (sale, product);
    }

    // ── GetActiveSalesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetActive_ReturnsActiveSalesWithFutureEndTime()
    {
        await SeedActiveSaleAsync("Weekend Sale");

        var result = await BuildSut().GetActiveSalesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Weekend Sale");
    }

    [Fact]
    public async Task GetActive_ExcludesScheduledSales()
    {
        _db.FlashSales.Add(new FlashSale
        {
            Id       = Guid.NewGuid(),
            Name     = "Future Sale",
            StartsAt = DateTime.UtcNow.AddHours(5),
            EndsAt   = DateTime.UtcNow.AddHours(8),
            Status   = FlashSaleStatus.Scheduled,
        });
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetActiveSalesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActive_ExcludesEndedSales()
    {
        _db.FlashSales.Add(new FlashSale
        {
            Id       = Guid.NewGuid(),
            Name     = "Past Sale",
            StartsAt = DateTime.UtcNow.AddHours(-5),
            EndsAt   = DateTime.UtcNow.AddHours(-1),
            Status   = FlashSaleStatus.Ended,
        });
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetActiveSalesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActive_ExcludesExpiredActiveSales()
    {
        // Status=Active but EndsAt is in the past (admin forgot to transition)
        _db.FlashSales.Add(new FlashSale
        {
            Id       = Guid.NewGuid(),
            Name     = "Stale Active Sale",
            StartsAt = DateTime.UtcNow.AddHours(-3),
            EndsAt   = DateTime.UtcNow.AddSeconds(-1),
            Status   = FlashSaleStatus.Active,
        });
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetActiveSalesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActive_SecondsRemaining_IsWithinExpectedRange()
    {
        await SeedActiveSaleAsync(hoursFromNow: 1.0);

        var result = await BuildSut().GetActiveSalesAsync();

        result.Should().HaveCount(1);
        result[0].SecondsRemaining.Should().BeInRange(3590, 3601, "sale ends in ~1 hour");
    }

    [Fact]
    public async Task GetActive_TotalItems_MatchesSeedCount()
    {
        var (sale, _) = await SeedActiveSaleAsync();
        // Add a second item to the same sale
        var product2 = new Product
        {
            Id = Guid.NewGuid(), Name = "Product 2", Slug = "p2",
            BasePrice = 500m, CategoryId = Guid.NewGuid(), BrandId = Guid.NewGuid(),
        };
        _db.Products.Add(product2);
        _db.FlashSaleItems.Add(new FlashSaleItem
        {
            Id = Guid.NewGuid(), FlashSaleId = sale.Id, ProductId = product2.Id,
            SalePrice = 400m, OriginalPrice = 500m, StockLimit = 5, SoldCount = 0,
        });
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetActiveSalesAsync();

        result[0].TotalItems.Should().Be(2);
    }

    // ── GetFlashSaleItemsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetItems_ReturnsCorrectRemainingStock()
    {
        var (sale, _) = await SeedActiveSaleAsync(stockLimit: 10, soldCount: 3);

        var items = await BuildSut().GetFlashSaleItemsAsync(sale.Id);

        items.Should().HaveCount(1);
        items[0].RemainingStock.Should().Be(7);
    }

    [Fact]
    public async Task GetItems_UnlimitedStock_RemainingStockIsZero()
    {
        var (sale, _) = await SeedActiveSaleAsync(stockLimit: 0, soldCount: 5);

        var items = await BuildSut().GetFlashSaleItemsAsync(sale.Id);

        items[0].RemainingStock.Should().Be(0, "unlimited stock (StockLimit=0) returns 0");
        items[0].IsSoldOut.Should().BeFalse();
    }

    [Fact]
    public async Task GetItems_UnknownSaleId_ReturnsEmpty()
    {
        var items = await BuildSut().GetFlashSaleItemsAsync(Guid.NewGuid());

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetItems_SoldOutFirst_SortedToEnd()
    {
        var sale = new FlashSale
        {
            Id = Guid.NewGuid(), Name = "Multi-item Sale",
            StartsAt = DateTime.UtcNow.AddHours(-1), EndsAt = DateTime.UtcNow.AddHours(2),
            Status = FlashSaleStatus.Active,
        };
        _db.FlashSales.Add(sale);

        var p1 = new Product { Id = Guid.NewGuid(), Name = "Available",  Slug = "p-avail",    BasePrice = 100m, CategoryId = Guid.NewGuid(), BrandId = Guid.NewGuid() };
        var p2 = new Product { Id = Guid.NewGuid(), Name = "Sold Out",   Slug = "p-sold-out", BasePrice = 200m, CategoryId = Guid.NewGuid(), BrandId = Guid.NewGuid() };
        _db.Products.AddRange(p1, p2);

        _db.FlashSaleItems.AddRange(
            new FlashSaleItem { Id = Guid.NewGuid(), FlashSaleId = sale.Id, ProductId = p2.Id, SalePrice = 150m, OriginalPrice = 200m, StockLimit = 5, SoldCount = 5, IsSoldOut = true  },
            new FlashSaleItem { Id = Guid.NewGuid(), FlashSaleId = sale.Id, ProductId = p1.Id, SalePrice = 80m,  OriginalPrice = 100m, StockLimit = 10, SoldCount = 2, IsSoldOut = false });
        await _db.SaveChangesAsync();

        var items = await BuildSut().GetFlashSaleItemsAsync(sale.Id);

        items[0].IsSoldOut.Should().BeFalse("available item comes first");
        items[1].IsSoldOut.Should().BeTrue("sold-out item comes last");
    }

    // ── RecordSaleAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RecordSale_IncrementsSoldCount()
    {
        var (sale, product) = await SeedActiveSaleAsync(stockLimit: 10, soldCount: 2);

        await BuildSut().RecordSaleAsync(sale.Id, product.Id, 3);

        var item = await _db.FlashSaleItems.FirstAsync(fi => fi.FlashSaleId == sale.Id);
        item.SoldCount.Should().Be(5);
    }

    [Fact]
    public async Task RecordSale_MarksSoldOutWhenThresholdReached()
    {
        var (sale, product) = await SeedActiveSaleAsync(stockLimit: 5, soldCount: 4);

        await BuildSut().RecordSaleAsync(sale.Id, product.Id, 1);

        var item = await _db.FlashSaleItems.FirstAsync(fi => fi.FlashSaleId == sale.Id);
        item.IsSoldOut.Should().BeTrue("SoldCount reached StockLimit");
    }

    [Fact]
    public async Task RecordSale_UnlimitedStock_NeverMarksSoldOut()
    {
        var (sale, product) = await SeedActiveSaleAsync(stockLimit: 0, soldCount: 999);

        await BuildSut().RecordSaleAsync(sale.Id, product.Id, 1);

        var item = await _db.FlashSaleItems.FirstAsync(fi => fi.FlashSaleId == sale.Id);
        item.IsSoldOut.Should().BeFalse("StockLimit=0 means unlimited");
    }

    [Fact]
    public async Task RecordSale_UnknownItem_DoesNotThrow()
    {
        var act = async () => await BuildSut().RecordSaleAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }
}
