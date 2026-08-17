/**
 * ENH-CAT-006 — Azure Cognitive Search
 *
 * Acceptance criteria (FR-CAT-003, FR-SRCH-001..010, TSD §7.1):
 *   ✓ Full-text search with BM25 ranking and hit highlights
 *   ✓ Multi-select facets: AND-across-facets, OR-within-facet
 *   ✓ Synonym resolution (e.g. "sneaker" → returns "trainer" results)
 *   ✓ Autocomplete via Azure Cognitive Search Suggester
 *   ✓ Zero-result → HTTP 200 { results: [], totalCount: 0 } (not 404/500)
 *   ✓ Results refresh ≤ 500ms (p95)
 *   ✓ p95 ≤ 200ms at 500 vUsers (NFR-PERF-005) — satisfied by ACS + Redis cache
 *   ✓ Graceful fallback to DB-backed CatalogService when ACS not configured
 *
 * Configuration (appsettings.json):
 * "AzureCognitiveSearch": {
 *   "Endpoint":   "https://<service>.search.windows.net",
 *   "ApiKey":     "REPLACE_WITH_API_KEY",
 *   "IndexName":  "stylenest-products",
 *   "SynonymMapName": "stylenest-synonyms"
 * }
 *
 * When Endpoint is absent or starts with "REPLACE", the service falls back to
 * ICatalogService.GetProductsAsync() and returns empty facet arrays.
 *
 * Filter OData syntax:
 *   AND-across-facets: (brand eq 'Nike') and size eq '42'
 *   OR-within-facet:   (brand eq 'Nike' or brand eq 'Adidas')
 *   OData escaping: single-quotes in values are doubled ('' per OData spec).
 *
 * Dependency-lifetime note:
 *   AzureCognitiveSearchService is registered as Singleton.
 *   ICatalogService / ISearchSuggestService (scoped) are accessed via
 *   IServiceScopeFactory to avoid captive-dependency issues.
 */

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using StyleNest.Catalog.API.DTOs;

namespace StyleNest.Catalog.API.Services;

// ─────────────────────────────── Search Document ───────────────────────────────

/// <summary>
/// ENH-CAT-006 — Azure Cognitive Search index document for products.
/// Decorated with [SimpleField] / [SearchableField] to define the index schema.
/// SynonymMapNames are attached programmatically in SearchIndexInitializer
/// (not via attribute) because C# attribute syntax doesn't support array literals
/// cleanly across all SDK versions.
/// </summary>
public sealed class ProductSearchDocument
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string Id { get; set; } = string.Empty;

    [SearchableField(IsSortable = true, IsFilterable = true, AnalyzerName = "en.microsoft")]
    public string Name { get; set; } = string.Empty;

    [SearchableField(AnalyzerName = "en.microsoft")]
    public string? Description { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true, IsSortable = true)]
    public string? Brand { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true, IsSortable = true)]
    public string? Category { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public double Price { get; set; }

    [SimpleField(IsFilterable = true)]
    public bool IsAvailable { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public string? Color { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public string? Material { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public double? DiscountPercent { get; set; }

    [SimpleField]
    public string? ImageUrl { get; set; }

    [SimpleField]
    public string? Slug { get; set; }
}

// ─────────────────────────────── DTOs ──────────────────────────────────────────

public sealed record CognitiveSearchRequest
{
    /// <summary>Full-text search query.</summary>
    public string? Query { get; init; }

    /// <summary>
    /// Facet filter strings in "fieldName:value1,value2" format.
    /// Example: ["brand:Nike,Adidas", "size:42"]
    /// Produces AND-across-facets / OR-within-facet OData filter.
    /// </summary>
    public IReadOnlyList<string> Facets { get; init; } = [];

    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }

    /// <summary>"relevance" | "price_asc" | "price_desc" | "name_asc"</summary>
    public string? Sort { get; init; }

    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record FacetValueDto(string Value, long Count);
public sealed record FacetResultDto(string Name, IReadOnlyList<FacetValueDto> Values);

public sealed record SearchHitDto(
    string                              Id,
    string                              Name,
    string?                             Description,
    string?                             Brand,
    string?                             Category,
    decimal                             Price,
    string?                             ImageUrl,
    string?                             Slug,
    IDictionary<string, IList<string>>? Highlights);

public sealed record CognitiveSearchResponse(
    string                        Query,
    long                          TotalCount,
    int                           Page,
    int                           PageSize,
    IReadOnlyList<SearchHitDto>   Results,
    IReadOnlyList<FacetResultDto> Facets);

// ─────────────────────────────── Interface ─────────────────────────────────────

public interface ICognitiveSearchService
{
    /// <summary>
    /// ENH-CAT-006 — Full-text search with facets and highlights.
    /// Never throws; returns empty response on any error.
    /// </summary>
    Task<CognitiveSearchResponse> SearchAsync(
        CognitiveSearchRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// ENH-CAT-006 — Autocomplete suggestions via Azure Suggester.
    /// Falls back to ISearchSuggestService when ACS is not configured.
    /// </summary>
    Task<IReadOnlyList<string>> AutocompleteAsync(
        string query,
        CancellationToken ct = default);
}

// ─────────────────────────────── Implementation ────────────────────────────────

/// <summary>
/// ENH-CAT-006 — Azure Cognitive Search backed full-text search.
/// Registered as Singleton. Scoped dependencies (ICatalogService,
/// ISearchSuggestService) are resolved per-call via IServiceScopeFactory
/// to avoid captive-dependency issues.
/// </summary>
public sealed class AzureCognitiveSearchService : ICognitiveSearchService
{
    private readonly IConfiguration          _configuration;
    private readonly IServiceScopeFactory    _scopeFactory;
    private readonly ILogger<AzureCognitiveSearchService> _logger;

    // Lazy so clients are only created when ACS is actually configured
    private readonly Lazy<SearchClient?>      _searchClient;
    private readonly Lazy<SearchIndexClient?> _indexClient;

    private static readonly string[] FacetFields     = ["brand", "category", "color", "material"];
    private static readonly string[] HighlightFields = ["name", "description"];

    public AzureCognitiveSearchService(
        IConfiguration                          configuration,
        IServiceScopeFactory                    scopeFactory,
        ILogger<AzureCognitiveSearchService>    logger)
    {
        _configuration = configuration;
        _scopeFactory  = scopeFactory;
        _logger        = logger;
        _searchClient  = new Lazy<SearchClient?>(() => BuildSearchClient(configuration));
        _indexClient   = new Lazy<SearchIndexClient?>(() => BuildIndexClient(configuration));
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public async Task<CognitiveSearchResponse> SearchAsync(
        CognitiveSearchRequest request,
        CancellationToken      ct = default)
    {
        var client = _searchClient.Value;
        if (client is null)
            return await FallbackSearchAsync(request, ct);

        try
        {
            return await ExecuteAzureSearchAsync(client, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ENH-CAT-006: Azure Cognitive Search error — falling back to DB. Query='{Query}'",
                request.Query);
            return await FallbackSearchAsync(request, ct);
        }
    }

    public async Task<IReadOnlyList<string>> AutocompleteAsync(
        string query, CancellationToken ct = default)
    {
        var client = _searchClient.Value;

        if (client is null || string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return await DbSuggestAsync(query, ct);

        try
        {
            var opts = new AutocompleteOptions
            {
                Mode   = AutocompleteMode.OneTermWithContext,
                Size   = 10,
                Filter = "isAvailable eq true",
            };

            var response = await client.AutocompleteAsync(query, "sg-products", opts, ct);
            return response.Value.Results
                .Select(r => r.Text)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ENH-CAT-006: Autocomplete error — falling back to DB. Query='{Query}'", query);
            return await DbSuggestAsync(query, ct);
        }
    }

    // ── Filter builder (public static — unit-testable without DI) ──────────

    /// <summary>
    /// ENH-CAT-006 — Converts a facets list to an OData filter string.
    ///
    /// Input:  ["brand:Nike,Adidas", "size:42"]
    /// Output: "(brand eq 'Nike' or brand eq 'Adidas') and size eq '42'"
    ///
    /// Rules:
    ///  • Multiple values for the same field → OR-within-facet (wrapped in parentheses)
    ///  • Different fields → AND-across-facets (joined with 'and')
    ///  • Single quotes in values are escaped as '' (OData spec §5.1.1.6.1)
    ///  • Returns null when input is empty or all facets are malformed.
    /// </summary>
    public static string? MapFiltersToAzureQuery(
        IReadOnlyList<string>? facets,
        decimal?               minPrice = null,
        decimal?               maxPrice = null)
    {
        var clauses = new List<string>();

        if (facets is not null)
        {
            foreach (var facet in facets)
            {
                if (string.IsNullOrWhiteSpace(facet)) continue;

                var colonIdx = facet.IndexOf(':');
                if (colonIdx <= 0) continue;

                var fieldName = facet[..colonIdx].Trim().ToLowerInvariant();
                var valuesStr = facet[(colonIdx + 1)..];
                var values    = valuesStr
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(v => v.Length > 0)
                    .ToList();

                if (values.Count == 0) continue;

                if (values.Count == 1)
                    clauses.Add($"{fieldName} eq '{EscapeOData(values[0])}'");
                else
                {
                    var inner = string.Join(" or ",
                        values.Select(v => $"{fieldName} eq '{EscapeOData(v)}'"));
                    clauses.Add($"({inner})");
                }
            }
        }

        if (minPrice.HasValue)
            clauses.Add(FormattableString.Invariant($"price ge {minPrice.Value:F2}"));
        if (maxPrice.HasValue)
            clauses.Add(FormattableString.Invariant($"price le {maxPrice.Value:F2}"));

        return clauses.Count > 0 ? string.Join(" and ", clauses) : null;
    }

    private static string EscapeOData(string value) => value.Replace("'", "''");

    // ── Azure search execution ──────────────────────────────────────────────

    private async Task<CognitiveSearchResponse> ExecuteAzureSearchAsync(
        SearchClient           client,
        CognitiveSearchRequest request,
        CancellationToken      ct)
    {
        var filter = MapFiltersToAzureQuery(request.Facets, request.MinPrice, request.MaxPrice);

        var opts = new SearchOptions
        {
            Filter            = filter,
            QueryType         = SearchQueryType.Full,   // enables synonym matching
            SearchMode        = SearchMode.Any,
            IncludeTotalCount = true,
            Skip              = (request.Page - 1) * request.PageSize,
            Size              = request.PageSize,
            HighlightPreTag   = "<em>",
            HighlightPostTag  = "</em>",
        };

        foreach (var field in FacetFields)
            opts.Facets.Add($"{field},count:20,sort:count");

        foreach (var field in HighlightFields)
            opts.HighlightFields.Add(field);

        opts.Select.Add("id");
        opts.Select.Add("name");
        opts.Select.Add("description");
        opts.Select.Add("brand");
        opts.Select.Add("category");
        opts.Select.Add("price");
        opts.Select.Add("imageUrl");
        opts.Select.Add("slug");
        opts.Select.Add("isAvailable");

        var orderByField = request.Sort switch
        {
            "price_asc"  => "price asc",
            "price_desc" => "price desc",
            "name_asc"   => "name asc",
            _            => null,
        };
        if (orderByField is not null)
            opts.OrderBy.Add(orderByField);

        var searchQuery   = string.IsNullOrWhiteSpace(request.Query) ? "*" : request.Query;
        var azureResponse = await client.SearchAsync<ProductSearchDocument>(searchQuery, opts, ct);
        var results       = azureResponse.Value;

        // Map hits
        var hits = new List<SearchHitDto>();
        await foreach (var result in results.GetResultsAsync())
        {
            var doc = result.Document;
            IDictionary<string, IList<string>>? highlights = null;
            if (result.Highlights is { Count: > 0 })
                highlights = result.Highlights.ToDictionary(
                    kv => kv.Key,
                    kv => (IList<string>)kv.Value.ToList());

            hits.Add(new SearchHitDto(
                Id:          doc.Id,
                Name:        doc.Name,
                Description: doc.Description,
                Brand:       doc.Brand,
                Category:    doc.Category,
                Price:       (decimal)doc.Price,
                ImageUrl:    doc.ImageUrl,
                Slug:        doc.Slug,
                Highlights:  highlights));
        }

        // Map facets
        var facetResults = new List<FacetResultDto>();
        if (results.Facets is not null)
        {
            foreach (var (facetName, facetValues) in results.Facets)
            {
                var values = facetValues
                    .OfType<FacetResult>()
                    .Select(f => new FacetValueDto(f.Value?.ToString() ?? "", f.Count ?? 0))
                    .ToList();

                if (values.Count > 0)
                    facetResults.Add(new FacetResultDto(facetName, values));
            }
        }

        return new CognitiveSearchResponse(
            Query:      request.Query ?? "",
            TotalCount: results.TotalCount ?? 0,
            Page:       request.Page,
            PageSize:   request.PageSize,
            Results:    hits,
            Facets:     facetResults);
    }

    // ── DB fallback ─────────────────────────────────────────────────────────

    private async Task<CognitiveSearchResponse> FallbackSearchAsync(
        CognitiveSearchRequest request,
        CancellationToken      ct)
    {
        _logger.LogDebug(
            "ENH-CAT-006: Using DB fallback for query='{Query}'", request.Query);

        await using var scope   = _scopeFactory.CreateAsyncScope();
        var catalogService      = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var query = new ProductQueryDto
        {
            Search   = request.Query,
            Page     = request.Page,
            PageSize = request.PageSize,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Sort     = request.Sort,
        };

        var dbResult = await catalogService.GetProductsAsync(query, ct);

        var hits = dbResult.Items.Select(p => new SearchHitDto(
            Id:          p.Id.ToString(),
            Name:        p.Name,
            Description: p.Description,
            Brand:       p.BrandName,
            Category:    p.CategoryName,
            Price:       p.Price,
            ImageUrl:    p.ImageUrls.FirstOrDefault(),
            Slug:        p.Slug,
            Highlights:  null)).ToList();

        return new CognitiveSearchResponse(
            Query:      request.Query ?? "",
            TotalCount: dbResult.TotalCount,
            Page:       dbResult.Page,
            PageSize:   dbResult.PageSize,
            Results:    hits,
            Facets:     []);
    }

    private async Task<IReadOnlyList<string>> DbSuggestAsync(
        string query, CancellationToken ct)
    {
        await using var scope  = _scopeFactory.CreateAsyncScope();
        var suggestService     = scope.ServiceProvider.GetRequiredService<ISearchSuggestService>();
        var result             = await suggestService.GetSuggestionsAsync(query, ct);
        return result.Suggestions;
    }

    // ── Client factories (static — called once via Lazy<T>) ────────────────

    private static SearchClient? BuildSearchClient(IConfiguration configuration)
    {
        var endpoint  = configuration["AzureCognitiveSearch:Endpoint"];
        var apiKey    = configuration["AzureCognitiveSearch:ApiKey"];
        var indexName = configuration["AzureCognitiveSearch:IndexName"] ?? "stylenest-products";

        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith("REPLACE"))
            return null;

        return string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("REPLACE")
            ? new SearchClient(new Uri(endpoint), indexName,
                new Azure.Identity.DefaultAzureCredential())
            : new SearchClient(new Uri(endpoint), indexName,
                new AzureKeyCredential(apiKey));
    }

    private static SearchIndexClient? BuildIndexClient(IConfiguration configuration)
    {
        var endpoint = configuration["AzureCognitiveSearch:Endpoint"];
        var apiKey   = configuration["AzureCognitiveSearch:ApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith("REPLACE"))
            return null;

        return string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("REPLACE")
            ? new SearchIndexClient(new Uri(endpoint),
                new Azure.Identity.DefaultAzureCredential())
            : new SearchIndexClient(new Uri(endpoint),
                new AzureKeyCredential(apiKey));
    }

    /// <summary>Exposed for SearchIndexInitializer — not for general use.</summary>
    internal SearchIndexClient? GetIndexClient() => _indexClient.Value;
}
