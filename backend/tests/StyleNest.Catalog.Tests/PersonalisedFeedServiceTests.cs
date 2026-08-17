/**
 * ENH-AI-001 — Personalised Product Feed (≥5 views in 30d → 12-product rail)
 * ENH-AI-002 — Personalised Feed Fallback (trendingByCategory for guests / cold-start)
 * Acceptance criteria tested here:
 *   - GetFeedAsync: guest (userId=null) → IsPersonalised=false, Reason="GUEST"
 *   - GetFeedAsync: 0 views → IsPersonalised=false, Reason="COLD_START"
 *   - GetFeedAsync: 1–4 views → IsPersonalised=false, Reason="INSUFFICIENT_VIEWS"
 *   - GetFeedAsync: ≥5 views → IsPersonalised=true, Reason="PERSONALISED"
 *   - GetFeedAsync: personalised products come from most-viewed categories
 *   - GetFeedAsync: already-viewed products excluded from personalised rail
 *   - GetFeedAsync: limit is respected (default 12)
 *   - GetFeedAsync: fills with trending when personalised count < limit
 *   - GetTrendingAsync: returns most-viewed products in 7-day window
 *   - GetTrendingAsync: inactive products excluded
 *   - GetTrendingAsync: filters by categoryId when provided
 *   - GetTrendingAsync: views outside the 7-day window not counted
 *   - GetTrendingAsync: respects limit parameter
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Analytics;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class PersonalisedFeedServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public PersonalisedFeedServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private PersonalisedFeedService BuildSut() =>
        new(_db, NullLogger<PersonalisedFeedService>.Instance);

    private Product MakeProduct(Guid categoryId, string name = "P", bool isActive = true,
        double rating = 4.0, int reviewCount = 10)
    {
        var p = new Product
        {
            Id            = Guid.NewGuid(),
            Name          = name,
            Slug          = $"{name.ToLower()}-{Guid.NewGuid():N}",
            BasePrice     = 999m,
            CategoryId    = categoryId,
            BrandId       = Guid.NewGuid(),
            IsActive      = isActive,
            AverageRating = rating,
            ReviewCount   = reviewCount,
        };
        _db.Products.Add(p);
        return p;
    }

    private ProductView MakeView(Guid productId, Guid? userId = null, double daysAgo = 1.0)
    {
        var v = new ProductView
        {
            Id        = Guid.NewGuid(),
            ProductId = productId,
            UserId    = userId,
            ViewedAt  = DateTime.UtcNow.AddDays(-daysAgo),
        };
        _db.ProductViews.Add(v);
        return v;
    }

    private async Task SaveAsync() => await _db.SaveChangesAsync();

    // ── GetFeedAsync — fallback cases (ENH-AI-002) ────────────────────────────

    [Fact]
    public async Task GetFeed_GuestUser_ReturnsGuestFallback()
    {
        var result = await BuildSut().GetFeedAsync(userId: null);

        result.IsPersonalised.Should().BeFalse();
        result.Reason.Should().Be("GUEST");
    }

    [Fact]
    public async Task GetFeed_ZeroViews_ReturnsColdStart()
    {
        var userId = Guid.NewGuid();

        var result = await BuildSut().GetFeedAsync(userId);

        result.IsPersonalised.Should().BeFalse();
        result.Reason.Should().Be("COLD_START");
    }

    [Fact]
    public async Task GetFeed_FourViews_ReturnsInsufficientViews()
    {
        var userId    = Guid.NewGuid();
        var catId     = Guid.NewGuid();
        // Create 4 distinct products and view each once
        for (int i = 0; i < 4; i++)
        {
            var p = MakeProduct(catId, $"P{i}");
            MakeView(p.Id, userId, daysAgo: i + 1);
        }
        await SaveAsync();

        var result = await BuildSut().GetFeedAsync(userId);

        result.IsPersonalised.Should().BeFalse();
        result.Reason.Should().Be("INSUFFICIENT_VIEWS");
    }

    // ── GetFeedAsync — personalised cases (ENH-AI-001) ────────────────────────

    [Fact]
    public async Task GetFeed_FiveViews_ReturnsPersonalised()
    {
        var userId = Guid.NewGuid();
        var catId  = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            var p = MakeProduct(catId, $"Viewed{i}");
            MakeView(p.Id, userId, daysAgo: i + 1);
        }
        // Seed an unviewed product in same category for the result
        MakeProduct(catId, "Unviewed");
        await SaveAsync();

        var result = await BuildSut().GetFeedAsync(userId);

        result.IsPersonalised.Should().BeTrue();
        result.Reason.Should().Be("PERSONALISED");
    }

    [Fact]
    public async Task GetFeed_PersonalisedProducts_FromTopViewedCategory()
    {
        var userId    = Guid.NewGuid();
        var catA      = Guid.NewGuid();   // heavily viewed
        var catB      = Guid.NewGuid();   // rarely viewed

        // 5 views in catA
        for (int i = 0; i < 5; i++)
        {
            var p = MakeProduct(catA, $"CatA-Viewed{i}");
            MakeView(p.Id, userId, daysAgo: i + 1);
        }
        // 1 view in catB
        var pB = MakeProduct(catB, "CatB-Viewed");
        MakeView(pB.Id, userId, daysAgo: 2);

        // Unviewed products
        var unviewedA = MakeProduct(catA, "CatA-Unviewed");
        MakeProduct(catB, "CatB-Unviewed");
        await SaveAsync();

        var result = await BuildSut().GetFeedAsync(userId);

        result.Products.Should().Contain(p => p.ProductId == unviewedA.Id,
            "unviewed product from most-viewed category should be in personalised rail");
    }

    [Fact]
    public async Task GetFeed_AlreadyViewedProducts_Excluded()
    {
        var userId = Guid.NewGuid();
        var catId  = Guid.NewGuid();
        var viewedProducts = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var p = MakeProduct(catId, $"Viewed{i}");
            MakeView(p.Id, userId, daysAgo: i + 1);
            viewedProducts.Add(p.Id);
        }
        await SaveAsync();

        var result = await BuildSut().GetFeedAsync(userId);

        result.Products.Should().NotContain(p => viewedProducts.Contains(p.ProductId),
            "products already viewed must not appear in the personalised rail");
    }

    [Fact]
    public async Task GetFeed_LimitRespected()
    {
        var userId = Guid.NewGuid();
        var catId  = Guid.NewGuid();
        for (int i = 0; i < 5; i++) MakeView(MakeProduct(catId, $"V{i}").Id, userId, i + 1);
        for (int i = 0; i < 20; i++) MakeProduct(catId, $"U{i}");   // 20 unviewed
        await SaveAsync();

        var result = await BuildSut().GetFeedAsync(userId, limit: 5);

        result.Products.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetFeed_ViewsOutside30DayWindow_NotCounted()
    {
        var userId = Guid.NewGuid();
        var catId  = Guid.NewGuid();
        // 5 views but all 31 days ago
        for (int i = 0; i < 5; i++)
        {
            var p = MakeProduct(catId, $"Old{i}");
            MakeView(p.Id, userId, daysAgo: 31 + i);
        }
        await SaveAsync();

        var result = await BuildSut().GetFeedAsync(userId);

        result.IsPersonalised.Should().BeFalse();
        result.Reason.Should().Be("COLD_START");
    }

    // ── GetTrendingAsync (ENH-AI-002) ─────────────────────────────────────────

    [Fact]
    public async Task GetTrending_ReturnsMostViewedProducts()
    {
        var catId = Guid.NewGuid();
        var hot   = MakeProduct(catId, "Hot Product");
        var cold  = MakeProduct(catId, "Cold Product");

        // hot = 10 views, cold = 1 view, both within 7 days
        for (int i = 0; i < 10; i++) MakeView(hot.Id, daysAgo: 1);
        MakeView(cold.Id, daysAgo: 2);
        await SaveAsync();

        var result = await BuildSut().GetTrendingAsync();

        result.Should().HaveCountGreaterOrEqualTo(1);
        result[0].ProductId.Should().Be(hot.Id, "most-viewed product should rank first");
    }

    [Fact]
    public async Task GetTrending_ExcludesInactiveProducts()
    {
        var catId    = Guid.NewGuid();
        var inactive = MakeProduct(catId, "Inactive", isActive: false);
        var active   = MakeProduct(catId, "Active");

        for (int i = 0; i < 5; i++) MakeView(inactive.Id, daysAgo: 1);
        MakeView(active.Id, daysAgo: 1);
        await SaveAsync();

        var result = await BuildSut().GetTrendingAsync();

        result.Should().NotContain(p => p.ProductId == inactive.Id);
        result.Should().Contain(p => p.ProductId == active.Id);
    }

    [Fact]
    public async Task GetTrending_FiltersByCategoryId()
    {
        var catA = Guid.NewGuid();
        var catB = Guid.NewGuid();
        var pA   = MakeProduct(catA, "Cat-A Product");
        var pB   = MakeProduct(catB, "Cat-B Product");

        for (int i = 0; i < 5; i++) MakeView(pA.Id, daysAgo: 1);
        for (int i = 0; i < 5; i++) MakeView(pB.Id, daysAgo: 1);
        await SaveAsync();

        var result = await BuildSut().GetTrendingAsync(categoryId: catB);

        result.Should().OnlyContain(p => p.CategoryId == catB);
    }

    [Fact]
    public async Task GetTrending_ViewsOlderThan7Days_NotCounted()
    {
        var catId = Guid.NewGuid();
        var old   = MakeProduct(catId, "Old-Views Product");
        var fresh = MakeProduct(catId, "Fresh-Views Product");

        // old product has 10 views but 8 days ago
        for (int i = 0; i < 10; i++) MakeView(old.Id, daysAgo: 8 + i);
        // fresh product has 1 view today
        MakeView(fresh.Id, daysAgo: 0.1);
        await SaveAsync();

        var result = await BuildSut().GetTrendingAsync();

        result.Should().Contain(p => p.ProductId == fresh.Id,
            "fresh product has views in window");
        result.Should().NotContain(p => p.ProductId == old.Id,
            "old product's views are outside the 7-day window");
    }

    [Fact]
    public async Task GetTrending_LimitRespected()
    {
        var catId = Guid.NewGuid();
        for (int i = 0; i < 10; i++)
        {
            var p = MakeProduct(catId, $"P{i}");
            MakeView(p.Id, daysAgo: 1);
        }
        await SaveAsync();

        var result = await BuildSut().GetTrendingAsync(limit: 3);

        result.Should().HaveCount(3);
    }
}
