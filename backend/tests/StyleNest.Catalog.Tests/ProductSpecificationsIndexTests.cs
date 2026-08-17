/**
 * ENH-CAT-010 — JSON Column Persisted Computed Index (SpecificationsJson)
 * TSD §6 / PC-002
 *
 * Acceptance criteria tested here:
 *
 *   TC-CAT-010-01: SpecificationsJson column is nvarchar(max) and nullable
 *   TC-CAT-010-02: SpecMaterial is a persisted computed column (IsStored = true)
 *   TC-CAT-010-03: SpecMaterial computed SQL contains JSON_VALUE expression
 *   TC-CAT-010-04: IX_Products_SpecMaterial index exists with correct name
 *   TC-CAT-010-05: IX_Products_SpecMaterial is a filtered index (WHERE SpecMaterial IS NOT NULL)
 *   TC-CAT-010-06: SpecMaterial is nullable (no non-null constraint)
 *   TC-CAT-010-07: Products can be saved and retrieved with SpecificationsJson JSON blob
 *   TC-CAT-010-08: Products with null SpecificationsJson are valid (nullable column)
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class ProductSpecificationsIndexTests : IDisposable
{
    private readonly AppDbContext _db;

    private static readonly Guid BrandId    = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    public ProductSpecificationsIndexTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the design-time (SQL Server-metadata-aware) model.</summary>
    private IModel DesignTimeModel =>
        _db.GetInfrastructure().GetRequiredService<IDesignTimeModel>().Model;

    private IEntityType ProductEntityType =>
        DesignTimeModel.FindEntityType(typeof(Product))
        ?? throw new InvalidOperationException("Product entity type not found in design-time model.");

    private async Task<Product> SeedProductAsync(string? specificationsJson = null)
    {
        // Seed Category and Brand if they don't exist yet
        if (!await _db.Categories.AnyAsync(c => c.Id == CategoryId))
        {
            _db.Categories.Add(new Category
            {
                Id           = CategoryId,
                Name         = "Test Category",
                Slug         = "test-category",
                DisplayOrder = 0,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow,
            });
        }
        if (!await _db.Brands.AnyAsync(b => b.Id == BrandId))
        {
            _db.Brands.Add(new Brand
            {
                Id        = BrandId,
                Name      = "Test Brand",
                Slug      = "test-brand",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        var product = new Product
        {
            Id                 = Guid.NewGuid(),
            Name               = "Test Product",
            Slug               = $"test-product-{Guid.NewGuid():N}",
            BasePrice          = 999m,
            CategoryId         = CategoryId,
            BrandId            = BrandId,
            IsActive           = true,
            SpecificationsJson = specificationsJson,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow,
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    // ── TC-CAT-010-01: SpecificationsJson is nvarchar(max) and nullable ───────

    [Fact]
    public void SpecificationsJson_Column_IsNvarcharMaxAndNullable()
    {
        var prop = ProductEntityType.FindProperty(nameof(Product.SpecificationsJson));

        prop.Should().NotBeNull(because: "TC-CAT-010-01: SpecificationsJson must be mapped to the Product entity");

        // GetColumnType() fails on InMemory (casts to RelationalTypeMapping internally).
        // Read the "Relational:ColumnType" annotation directly — it is set regardless of provider.
        var columnType = prop!.FindAnnotation("Relational:ColumnType")?.Value?.ToString();
        columnType.Should().Be("nvarchar(max)",
            because: "TC-CAT-010-01: SpecificationsJson must be nvarchar(max) to store arbitrary JSON");

        prop.IsNullable.Should().BeTrue(
            because: "TC-CAT-010-01: products without specifications must be allowed");
    }

    // ── TC-CAT-010-02: SpecMaterial is a persisted computed column ────────────

    [Fact]
    public void SpecMaterial_IsPersistedComputedColumn()
    {
        var prop = ProductEntityType.FindProperty(nameof(Product.SpecMaterial));

        prop.Should().NotBeNull(because: "TC-CAT-010-02: SpecMaterial must be mapped");
        prop!.GetIsStored().Should().BeTrue(
            because: "TC-CAT-010-02: PERSISTED computed columns must have IsStored=true so they are physically stored on disk and indexable");
    }

    // ── TC-CAT-010-03: SpecMaterial computed SQL contains JSON_VALUE ──────────

    [Fact]
    public void SpecMaterial_ComputedColumnSql_ContainsJsonValue()
    {
        var prop = ProductEntityType.FindProperty(nameof(Product.SpecMaterial));

        prop.Should().NotBeNull();
        var sql = prop!.GetComputedColumnSql() ?? string.Empty;
        sql.Should().NotBeNullOrEmpty(
            because: "TC-CAT-010-03: SpecMaterial must have a computed column SQL expression");
        sql.Should().Contain("JSON_VALUE",
            because: "TC-CAT-010-03: the computed expression must extract the 'material' key from SpecificationsJson using JSON_VALUE");
        sql.Should().Contain("material",
            because: "TC-CAT-010-03: the JSON path must target the '$.material' key");
    }

    // ── TC-CAT-010-04: IX_Products_SpecMaterial index exists ─────────────────

    [Fact]
    public void SpecMaterial_HasIndex_WithCorrectName()
    {
        var index = ProductEntityType.GetIndexes()
            .FirstOrDefault(ix =>
                ix.Properties.Any(p => p.Name == nameof(Product.SpecMaterial)));

        index.Should().NotBeNull(
            because: "TC-CAT-010-04: an index on SpecMaterial must exist for O(log n) material queries");
        index!.GetDatabaseName().Should().Be("IX_Products_SpecMaterial",
            because: "TC-CAT-010-04: the index must follow the project naming convention");
    }

    // ── TC-CAT-010-05: IX_Products_SpecMaterial is a filtered index ──────────

    [Fact]
    public void SpecMaterial_Index_HasNullFilter()
    {
        var index = ProductEntityType.GetIndexes()
            .FirstOrDefault(ix =>
                ix.Properties.Any(p => p.Name == nameof(Product.SpecMaterial)));

        index.Should().NotBeNull();
        var filter = index!.GetFilter();
        filter.Should().NotBeNullOrEmpty(
            because: "TC-CAT-010-05: a filtered index avoids storing rows with NULL SpecMaterial, keeping it selective");
        filter.Should().Contain("SpecMaterial",
            because: "TC-CAT-010-05: the WHERE clause must reference SpecMaterial");
        filter.Should().Contain("IS NOT NULL",
            because: "TC-CAT-010-05: only rows WITH a material value should be indexed");
    }

    // ── TC-CAT-010-06: SpecMaterial is nullable ───────────────────────────────

    [Fact]
    public void SpecMaterial_IsNullable()
    {
        var prop = ProductEntityType.FindProperty(nameof(Product.SpecMaterial));

        prop.Should().NotBeNull();
        prop!.IsNullable.Should().BeTrue(
            because: "TC-CAT-010-06: products without a material spec must be stored with SpecMaterial=NULL");
    }

    // ── TC-CAT-010-07: save and retrieve SpecificationsJson ──────────────────

    [Fact]
    public async Task Product_SaveAndRetrieve_SpecificationsJson()
    {
        const string json = """{"material":"100% Cotton","fit":"Slim Fit","pattern":"Solid"}""";

        var saved = await SeedProductAsync(json);

        var fromDb = await _db.Products.AsNoTracking()
            .FirstAsync(p => p.Id == saved.Id);

        fromDb.SpecificationsJson.Should().Be(json,
            because: "TC-CAT-010-07: the full JSON blob must round-trip through the nvarchar(max) column unchanged");
    }

    // ── TC-CAT-010-08: null SpecificationsJson is valid ──────────────────────

    [Fact]
    public async Task Product_WithNullSpecificationsJson_SavesSuccessfully()
    {
        var saved = await SeedProductAsync(specificationsJson: null);

        var fromDb = await _db.Products.AsNoTracking()
            .FirstAsync(p => p.Id == saved.Id);

        fromDb.SpecificationsJson.Should().BeNull(
            because: "TC-CAT-010-08: products without specs must persist with SpecificationsJson=NULL (column is nullable)");
    }
}
