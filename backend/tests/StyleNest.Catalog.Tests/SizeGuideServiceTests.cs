/**
 * ENH-PDP-003 — Size Guide Modal: SizeGuideService tests
 *
 * Acceptance criteria (FR-PDP-005):
 *   TC-PDP-003-01: GetAsync — returns category-specific guide when it exists
 *   TC-PDP-003-02: GetAsync — falls back to brand-wide guide when no category-specific one
 *   TC-PDP-003-03: GetAsync — returns null when no guide at all
 *   TC-PDP-003-04: GetAsync — returns null when guide is soft-deleted
 *   TC-PDP-003-05: UpsertAsync — creates new guide with correct fields
 *   TC-PDP-003-06: UpsertAsync — updates existing guide (GuideName + ChartJson)
 *   TC-PDP-003-07: UpsertAsync — revives soft-deleted guide on upsert
 *   TC-PDP-003-08: UpsertAsync — throws ArgumentException on invalid JSON
 *   TC-PDP-003-09: UpsertAsync — throws ArgumentException when columns array missing
 *   TC-PDP-003-10: UpsertAsync — stores brand-wide guide when categoryId = null
 *   TC-PDP-003-11: UpsertAsync — category-specific guide does not overwrite brand-wide
 *   TC-PDP-003-12: DeleteAsync — soft-deletes guide (IsDeleted = true)
 *   TC-PDP-003-13: DeleteAsync — idempotent when guide not found
 *   TC-PDP-003-14: GetAsync — category-specific guide preferred over brand-wide fallback
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class SizeGuideServiceTests : IDisposable
{
    private readonly AppDbContext  _db;
    private readonly SizeGuideService _svc;

    private static readonly Guid BrandId1  = Guid.Parse("BBBB0001-0000-0000-0000-000000000001");
    private static readonly Guid BrandId2  = Guid.Parse("BBBB0002-0000-0000-0000-000000000002");
    private static readonly Guid CatId1    = Guid.Parse("CCCC0001-0000-0000-0000-000000000001");
    private static readonly Guid CatId2    = Guid.Parse("CCCC0002-0000-0000-0000-000000000002");

    private const string ValidChart = """{"columns":["Size","Chest"],"rows":[["S","88"]]}""";
    private const string UpdatedChart = """{"columns":["Size","Waist"],"rows":[["M","76"]]}""";

    public SizeGuideServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _svc = new SizeGuideService(_db);
        SeedBrandsAndCategories();
    }

    public void Dispose() => _db.Dispose();

    // ─── GetAsync ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-003-01: GetAsync — returns category-specific guide")]
    public async Task Get_CategorySpecific_ReturnsGuide()
    {
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Cat Guide", ValidChart, CatId1));

        var dto = await _svc.GetAsync(BrandId1, CatId1);

        dto.Should().NotBeNull();
        dto!.CategoryId.Should().Be(CatId1);
        dto.GuideName.Should().Be("Cat Guide");
    }

    [Fact(DisplayName = "TC-PDP-003-02: GetAsync — falls back to brand-wide guide")]
    public async Task Get_Fallback_ReturnsBrandWide()
    {
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Brand Guide", ValidChart, null));

        // Request for CatId1 — no category-specific guide exists
        var dto = await _svc.GetAsync(BrandId1, CatId1);

        dto.Should().NotBeNull();
        dto!.CategoryId.Should().BeNull();
        dto.GuideName.Should().Be("Brand Guide");
    }

    [Fact(DisplayName = "TC-PDP-003-03: GetAsync — returns null when no guide")]
    public async Task Get_NoGuide_ReturnsNull()
    {
        var dto = await _svc.GetAsync(BrandId2, null);
        dto.Should().BeNull();
    }

    [Fact(DisplayName = "TC-PDP-003-04: GetAsync — returns null for soft-deleted guide")]
    public async Task Get_SoftDeleted_ReturnsNull()
    {
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Guide", ValidChart, null));
        await _svc.DeleteAsync(BrandId1, null);

        var dto = await _svc.GetAsync(BrandId1, null);
        dto.Should().BeNull();
    }

    // ─── UpsertAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-003-05: UpsertAsync — creates new guide with correct fields")]
    public async Task Upsert_Creates_CorrectFields()
    {
        var dto = await _svc.UpsertAsync(BrandId1,
            new UpsertSizeGuideRequest("Men's Tops", ValidChart, CatId1));

        dto.BrandId.Should().Be(BrandId1);
        dto.CategoryId.Should().Be(CatId1);
        dto.GuideName.Should().Be("Men's Tops");
        dto.ChartJson.Should().Be(ValidChart);
        dto.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "TC-PDP-003-06: UpsertAsync — updates existing guide")]
    public async Task Upsert_Updates_Existing()
    {
        var created = await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Old", ValidChart, null));

        var updated = await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("New", UpdatedChart, null));

        updated.Id.Should().Be(created.Id); // same row
        updated.GuideName.Should().Be("New");
        updated.ChartJson.Should().Be(UpdatedChart);
    }

    [Fact(DisplayName = "TC-PDP-003-07: UpsertAsync — revives soft-deleted guide")]
    public async Task Upsert_RevivesSoftDeleted()
    {
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Guide", ValidChart, null));
        await _svc.DeleteAsync(BrandId1, null);

        // Re-upsert should bring it back
        var dto = await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Revived", ValidChart, null));

        dto.GuideName.Should().Be("Revived");

        var fromDb = await _svc.GetAsync(BrandId1, null);
        fromDb.Should().NotBeNull();
    }

    [Fact(DisplayName = "TC-PDP-003-08: UpsertAsync — throws on invalid JSON")]
    public async Task Upsert_ThrowsOnInvalidJson()
    {
        var act = async () => await _svc.UpsertAsync(BrandId1,
            new UpsertSizeGuideRequest("Guide", "not-valid-json", null));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*valid JSON*");
    }

    [Fact(DisplayName = "TC-PDP-003-09: UpsertAsync — throws when columns array missing")]
    public async Task Upsert_ThrowsWhenColumnsMissing()
    {
        var act = async () => await _svc.UpsertAsync(BrandId1,
            new UpsertSizeGuideRequest("Guide", """{"rows":[["S"]]}""", null));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*columns*");
    }

    [Fact(DisplayName = "TC-PDP-003-10: UpsertAsync — stores brand-wide guide when categoryId null")]
    public async Task Upsert_BrandWide_CategoryIdNull()
    {
        var dto = await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Brand Guide", ValidChart, null));

        dto.CategoryId.Should().BeNull();

        var stored = await _db.SizeGuides.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.BrandId == BrandId1 && g.CategoryId == null);
        stored.Should().NotBeNull();
    }

    [Fact(DisplayName = "TC-PDP-003-11: UpsertAsync — category-specific does not overwrite brand-wide")]
    public async Task Upsert_CategorySpecific_DoesNotOverwriteBrandWide()
    {
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Brand", ValidChart, null));
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("CatGuide", UpdatedChart, CatId1));

        var brandWide = await _svc.GetAsync(BrandId1, null);
        brandWide.Should().NotBeNull();
        brandWide!.GuideName.Should().Be("Brand"); // unchanged
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-003-12: DeleteAsync — soft-deletes guide")]
    public async Task Delete_SoftDeletes()
    {
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Guide", ValidChart, null));
        await _svc.DeleteAsync(BrandId1, null);

        var stored = await _db.SizeGuides.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.BrandId == BrandId1 && g.CategoryId == null);
        stored!.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "TC-PDP-003-13: DeleteAsync — idempotent when not found")]
    public async Task Delete_Idempotent_NoThrow()
    {
        var act = async () => await _svc.DeleteAsync(BrandId2, null);
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "TC-PDP-003-14: GetAsync — category-specific preferred over brand-wide")]
    public async Task Get_CategorySpecific_PreferredOverBrandWide()
    {
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Brand Guide", ValidChart, null));
        await _svc.UpsertAsync(BrandId1, new UpsertSizeGuideRequest("Cat Guide",   UpdatedChart, CatId1));

        var dto = await _svc.GetAsync(BrandId1, CatId1);

        dto.Should().NotBeNull();
        dto!.GuideName.Should().Be("Cat Guide"); // category-specific wins
        dto.CategoryId.Should().Be(CatId1);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SeedBrandsAndCategories()
    {
        _db.Brands.AddRange(
            new Brand { Id = BrandId1, Name = "Brand One", Slug = "brand-one" },
            new Brand { Id = BrandId2, Name = "Brand Two", Slug = "brand-two" });
        _db.Categories.AddRange(
            new Category { Id = CatId1, Name = "Tops",    Slug = "tops"    },
            new Category { Id = CatId2, Name = "Bottoms", Slug = "bottoms" });
        _db.SaveChanges();
    }
}
