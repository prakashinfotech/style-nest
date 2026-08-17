/**
 * ENH-CAT-001 — Recently Viewed Products Rail (last 12 views per user)
 * Acceptance criteria tested here:
 *
 *   TC-CAT-001-01: RecordView inserts a new entry on first view
 *   TC-CAT-001-02: RecordView on already-viewed product updates ViewedAt (upsert — no duplicate)
 *   TC-CAT-001-03: Cap enforced — 13th distinct product view prunes oldest to keep 12
 *   TC-CAT-001-04: GetRecentlyViewed returns items ordered newest-first
 *   TC-CAT-001-05: GetRecentlyViewed respects the limit parameter
 *   TC-CAT-001-06: Cross-user isolation — User A never sees User B's views
 *   TC-CAT-001-07: Inactive (IsActive=false) products excluded from results
 *   TC-CAT-001-08: Session-scoped isolation — different sessionIds never mix
 *   TC-CAT-001-09: RecordView for a product that does not exist in the catalog does not throw
 *   TC-CAT-001-10: Re-viewing a product moves it to the front of the list
 *   TC-CAT-001-11: GetRecentlyViewed with no userId and no sessionId returns empty list
 *   TC-CAT-001-12: Soft-deleted products excluded from results
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class RecentlyViewedServiceTests : IDisposable
{
    private readonly AppDbContext       _db;
    private readonly RecentlyViewedService _sut;

    // Fixed reference time to make assertions deterministic
    private static readonly DateTime T0 = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

    public RecentlyViewedServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new RecentlyViewedService(_db, NullLogger<RecentlyViewedService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Product> SeedProductAsync(
        string name = "Test Product", bool isActive = true, bool isDeleted = false)
    {
        var product = new Product
        {
            Id            = Guid.NewGuid(),
            Name          = name,
            Slug          = name.ToLowerInvariant().Replace(' ', '-') + "-" + Guid.NewGuid().ToString("N")[..6],
            BasePrice     = 999m,
            CategoryId    = Guid.NewGuid(),
            BrandId       = Guid.NewGuid(),
            IsActive      = isActive,
            IsDeleted     = isDeleted,
            CreatedAt     = T0,
            UpdatedAt     = T0,
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    // ── TC-CAT-001-01: first view inserts a row ───────────────────────────────

    [Fact]
    public async Task RecordView_FirstView_InsertsRow()
    {
        var userId    = Guid.NewGuid();
        var product   = await SeedProductAsync();

        await _sut.RecordViewAsync(userId, null, product.Id, T0);

        var count = await _db.ProductViews.CountAsync(v => v.UserId == userId);
        count.Should().Be(1, because: "TC-CAT-001-01: first view must create exactly one row");
    }

    // ── TC-CAT-001-02: second view on same product updates ViewedAt ───────────

    [Fact]
    public async Task RecordView_SameProductTwice_UpdatesViewedAtNoDuplicate()
    {
        var userId  = Guid.NewGuid();
        var product = await SeedProductAsync();

        await _sut.RecordViewAsync(userId, null, product.Id, T0);
        await _sut.RecordViewAsync(userId, null, product.Id, T0.AddMinutes(30));

        var rows = await _db.ProductViews.Where(v => v.UserId == userId).ToListAsync();

        rows.Should().HaveCount(1,
            because: "TC-CAT-001-02: re-viewing the same product must upsert, not duplicate");
        rows[0].ViewedAt.Should().Be(T0.AddMinutes(30),
            because: "the ViewedAt timestamp must be refreshed to the latest view time");
    }

    // ── TC-CAT-001-03: cap at 12 — 13th view prunes oldest ───────────────────

    [Fact]
    public async Task RecordView_13thDistinctProduct_PrunesOldest()
    {
        var userId = Guid.NewGuid();
        var products = new List<Product>();
        for (int i = 0; i < 13; i++)
            products.Add(await SeedProductAsync($"Product {i}"));

        // Record 12 views at T0, T0+1min, …, T0+11min
        for (int i = 0; i < 12; i++)
            await _sut.RecordViewAsync(userId, null, products[i].Id, T0.AddMinutes(i));

        // Record 13th view — should prune the oldest (products[0], viewed at T0)
        await _sut.RecordViewAsync(userId, null, products[12].Id, T0.AddMinutes(12));

        var remaining = await _db.ProductViews
            .Where(v => v.UserId == userId)
            .OrderBy(v => v.ViewedAt)
            .ToListAsync();

        remaining.Should().HaveCount(RecentlyViewedService.MaxViews,
            because: "TC-CAT-001-03: the cap must be enforced after each upsert");

        remaining.Should().NotContain(v => v.ProductId == products[0].Id,
            because: "the oldest entry (products[0]) must have been pruned");
        remaining.Should().Contain(v => v.ProductId == products[12].Id,
            because: "the newest entry (13th product) must be retained");
    }

    // ── TC-CAT-001-04: results ordered newest-first ───────────────────────────

    [Fact]
    public async Task GetRecentlyViewed_OrderedNewestFirst()
    {
        var userId = Guid.NewGuid();
        var p1     = await SeedProductAsync("Alpha");
        var p2     = await SeedProductAsync("Beta");
        var p3     = await SeedProductAsync("Gamma");

        await _sut.RecordViewAsync(userId, null, p1.Id, T0);
        await _sut.RecordViewAsync(userId, null, p2.Id, T0.AddMinutes(5));
        await _sut.RecordViewAsync(userId, null, p3.Id, T0.AddMinutes(10));

        var items = await _sut.GetRecentlyViewedAsync(userId, null);

        items.Should().HaveCount(3);
        items[0].ProductId.Should().Be(p3.Id, because: "TC-CAT-001-04: newest view first");
        items[1].ProductId.Should().Be(p2.Id);
        items[2].ProductId.Should().Be(p1.Id, because: "oldest view last");
    }

    // ── TC-CAT-001-05: limit parameter respected ──────────────────────────────

    [Fact]
    public async Task GetRecentlyViewed_RespectsLimit()
    {
        var userId = Guid.NewGuid();
        for (int i = 0; i < 8; i++)
        {
            var p = await SeedProductAsync($"Product {i}");
            await _sut.RecordViewAsync(userId, null, p.Id, T0.AddMinutes(i));
        }

        var items = await _sut.GetRecentlyViewedAsync(userId, null, limit: 3);

        items.Should().HaveCount(3, because: "TC-CAT-001-05: limit=3 must cap the result");
    }

    // ── TC-CAT-001-06: cross-user isolation ───────────────────────────────────

    [Fact]
    public async Task GetRecentlyViewed_CrossUserIsolation()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var pA    = await SeedProductAsync("ProductA");
        var pB    = await SeedProductAsync("ProductB");

        await _sut.RecordViewAsync(userA, null, pA.Id, T0);
        await _sut.RecordViewAsync(userB, null, pB.Id, T0);

        var itemsA = await _sut.GetRecentlyViewedAsync(userA, null);
        var itemsB = await _sut.GetRecentlyViewedAsync(userB, null);

        itemsA.Should().ContainSingle().Which.ProductId.Should().Be(pA.Id,
            because: "TC-CAT-001-06: User A must only see their own views");
        itemsB.Should().ContainSingle().Which.ProductId.Should().Be(pB.Id,
            because: "User B must only see their own views");
    }

    // ── TC-CAT-001-07: inactive products excluded ─────────────────────────────

    [Fact]
    public async Task GetRecentlyViewed_InactiveProduct_Excluded()
    {
        var userId  = Guid.NewGuid();
        var active  = await SeedProductAsync("Active",   isActive: true);
        var inactive = await SeedProductAsync("Inactive", isActive: false);

        await _sut.RecordViewAsync(userId, null, active.Id,   T0);
        await _sut.RecordViewAsync(userId, null, inactive.Id, T0.AddMinutes(1));

        var items = await _sut.GetRecentlyViewedAsync(userId, null);

        items.Should().ContainSingle()
            .Which.ProductId.Should().Be(active.Id,
                because: "TC-CAT-001-07: inactive products must not appear in the rail");
    }

    // ── TC-CAT-001-08: session-scoped isolation ───────────────────────────────

    [Fact]
    public async Task GetRecentlyViewed_SessionIsolation_DifferentSessionsDoNotMix()
    {
        var pA = await SeedProductAsync("SessionA-Product");
        var pB = await SeedProductAsync("SessionB-Product");

        await _sut.RecordViewAsync(null, "sess-AAA", pA.Id, T0);
        await _sut.RecordViewAsync(null, "sess-BBB", pB.Id, T0);

        var itemsA = await _sut.GetRecentlyViewedAsync(null, "sess-AAA");
        var itemsB = await _sut.GetRecentlyViewedAsync(null, "sess-BBB");

        itemsA.Should().ContainSingle().Which.ProductId.Should().Be(pA.Id,
            because: "TC-CAT-001-08: sess-AAA must only see their own views");
        itemsB.Should().ContainSingle().Which.ProductId.Should().Be(pB.Id,
            because: "sess-BBB must only see their own views");
    }

    // ── TC-CAT-001-09: recording view for unknown product does not throw ───────

    [Fact]
    public async Task RecordView_NonExistentProduct_DoesNotThrow()
    {
        var userId = Guid.NewGuid();

        var act = async () =>
            await _sut.RecordViewAsync(userId, null, Guid.NewGuid(), T0);

        // View row is inserted even if product doesn't exist in catalog
        // (the join in Get will simply return nothing for it)
        await act.Should().NotThrowAsync(
            because: "TC-CAT-001-09: recording a view for a missing product must not crash");
    }

    // ── TC-CAT-001-10: re-viewing moves product to front ─────────────────────

    [Fact]
    public async Task RecordView_ReViewingProduct_MovesItToFront()
    {
        var userId = Guid.NewGuid();
        var p1     = await SeedProductAsync("First");
        var p2     = await SeedProductAsync("Second");
        var p3     = await SeedProductAsync("Third");

        await _sut.RecordViewAsync(userId, null, p1.Id, T0);
        await _sut.RecordViewAsync(userId, null, p2.Id, T0.AddMinutes(1));
        await _sut.RecordViewAsync(userId, null, p3.Id, T0.AddMinutes(2));

        // Re-view p1 — it should now be at the front
        await _sut.RecordViewAsync(userId, null, p1.Id, T0.AddMinutes(10));

        var items = await _sut.GetRecentlyViewedAsync(userId, null);

        items[0].ProductId.Should().Be(p1.Id,
            because: "TC-CAT-001-10: re-viewing a product must bring it to the front of the rail");
        items.Should().HaveCount(3, because: "no new row should have been created for the re-view");
    }

    // ── TC-CAT-001-11: no userId / no sessionId → empty list ─────────────────

    [Fact]
    public async Task GetRecentlyViewed_NoUserIdAndNoSessionId_ReturnsEmpty()
    {
        var items = await _sut.GetRecentlyViewedAsync(null, null);

        items.Should().BeEmpty(
            because: "TC-CAT-001-11: without a user or session context there is nothing to return");
    }

    // ── TC-CAT-001-12: soft-deleted products excluded ─────────────────────────

    [Fact]
    public async Task GetRecentlyViewed_SoftDeletedProduct_Excluded()
    {
        var userId  = Guid.NewGuid();
        var alive   = await SeedProductAsync("Alive",   isActive: true,  isDeleted: false);
        var deleted = await SeedProductAsync("Deleted", isActive: true,  isDeleted: true);

        await _sut.RecordViewAsync(userId, null, alive.Id,   T0);
        await _sut.RecordViewAsync(userId, null, deleted.Id, T0.AddMinutes(1));

        var items = await _sut.GetRecentlyViewedAsync(userId, null);

        items.Should().ContainSingle()
            .Which.ProductId.Should().Be(alive.Id,
                because: "TC-CAT-001-12: soft-deleted products must never appear in the rail");
    }
}
