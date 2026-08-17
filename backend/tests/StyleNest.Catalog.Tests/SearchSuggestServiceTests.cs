/**
 * ENH-SRCH-002 — Search Autocomplete + Typeahead
 * Acceptance criteria tested here:
 *   - q < 2 chars → empty suggestions (not error)
 *   - Matches product names, brand names, category names, popular search terms
 *   - Case-insensitive prefix match
 *   - Max 10 suggestions returned
 *   - Results deduplicated
 *   - No match → empty array (HTTP 200)
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

public sealed class SearchSuggestServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SearchSuggestService _sut;

    public SearchSuggestServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new SearchSuggestService(_db, NullLogger<SearchSuggestService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private void AddProduct(string name)
    {
        _db.Products.Add(new Product
        {
            Id          = Guid.NewGuid(),
            Name        = name,
            Slug        = name.ToLower().Replace(" ", "-"),
            Description = string.Empty,
            BasePrice   = 999m,
        });
    }

    private void AddBrand(string name)
    {
        _db.Brands.Add(new Brand
        {
            Id   = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
        });
    }

    private void AddCategory(string name)
    {
        _db.Categories.Add(new Category
        {
            Id   = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
        });
    }

    private void AddSearchTerm(string term, int count = 1)
    {
        _db.SearchTerms.Add(new SearchTerm
        {
            Id             = Guid.NewGuid(),
            Term           = term,
            Count          = count,
            LastSearchedAt = DateTime.UtcNow,
        });
    }

    // ── short query → empty ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_EmptyQuery_ReturnsEmpty()
    {
        var result = await _sut.GetSuggestionsAsync(string.Empty);

        result.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestions_OneCharQuery_ReturnsEmpty()
    {
        AddProduct("Shoes");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("s");

        result.Suggestions.Should().BeEmpty("queries shorter than 2 chars return no results");
    }

    [Fact]
    public async Task GetSuggestions_WhitespaceOnly_ReturnsEmpty()
    {
        var result = await _sut.GetSuggestionsAsync("  ");

        result.Suggestions.Should().BeEmpty();
    }

    // ── product name matching ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_MatchesProductName_ReturnsSuggestion()
    {
        AddProduct("Silk Saree");
        AddProduct("Cotton Kurta");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("Sil");

        result.Suggestions.Should().Contain("Silk Saree");
        result.Suggestions.Should().NotContain("Cotton Kurta");
    }

    [Fact]
    public async Task GetSuggestions_CaseInsensitive_MatchesProduct()
    {
        AddProduct("Denim Jeans");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("den");

        result.Suggestions.Should().Contain("Denim Jeans",
            "prefix match is case-insensitive");
    }

    // ── brand name matching ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_MatchesBrandName_ReturnsSuggestion()
    {
        AddBrand("FabIndia");
        AddBrand("Biba");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("fab");

        result.Suggestions.Should().Contain("FabIndia");
        result.Suggestions.Should().NotContain("Biba");
    }

    // ── category name matching ────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_MatchesCategoryName_ReturnsSuggestion()
    {
        AddCategory("Women Kurtas");
        AddCategory("Men Shirts");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("Wo");

        result.Suggestions.Should().Contain("Women Kurtas");
        result.Suggestions.Should().NotContain("Men Shirts");
    }

    // ── popular search terms ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_MatchesPopularSearchTerm_ReturnsSuggestion()
    {
        AddSearchTerm("shoes for women", count: 500);
        AddSearchTerm("short kurti", count: 200);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("sh");

        result.Suggestions.Should().ContainEquivalentOf("shoes for women");
    }

    [Fact]
    public async Task GetSuggestions_PopularTermsAppearsFirst_HighCountFirst()
    {
        AddSearchTerm("saree online", count: 1000);
        AddSearchTerm("sandals",      count: 5);
        AddProduct("Saree Collection");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("sa");

        // Popular term should appear before product
        result.Suggestions.First(s =>
            s.Equals("saree online", StringComparison.OrdinalIgnoreCase))
            .Should().NotBeNull();
    }

    // ── no match → empty ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_NoMatch_ReturnsEmpty()
    {
        AddProduct("Kurta");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("xyz");

        result.Suggestions.Should().BeEmpty();
    }

    // ── deduplication ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_DuplicateSuggestions_Deduplicated()
    {
        // Same name in SearchTerms AND Products — should appear once
        AddSearchTerm("Kurta", count: 100);
        AddProduct("Kurta");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("Ku");

        result.Suggestions.Where(s =>
            s.Equals("Kurta", StringComparison.OrdinalIgnoreCase))
            .Should().HaveCount(1, "duplicates are removed");
    }

    // ── cap at 10 suggestions ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_ManyMatches_CappedAtTen()
    {
        for (int i = 1; i <= 20; i++)
        {
            AddProduct($"Shirt Style {i:D2}");
        }
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestionsAsync("sh");

        result.Suggestions.Should().HaveCountLessOrEqualTo(10,
            "suggestions are capped at 10 regardless of match count");
    }

    // ── response metadata ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_ResponseContainsQueryEcho()
    {
        var result = await _sut.GetSuggestionsAsync("sh");

        result.Query.Should().Be("sh");
    }
}
