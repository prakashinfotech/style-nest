using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/search")]
public sealed class SearchController(
    ICognitiveSearchService cognitiveSearch,
    ISearchSuggestService   suggestService) : ControllerBase
{
    /// <summary>
    /// ENH-CAT-006 — Full-text product search via Azure Cognitive Search.
    ///
    /// Supports:
    ///   - BM25 relevance ranking with synonym expansion
    ///   - Multi-select facets: AND-across-facets, OR-within-facet
    ///   - Hit highlights in Name and Description fields
    ///   - Graceful fallback to DB when ACS is not configured
    ///   - Zero-result queries return HTTP 200 { results: [], totalCount: 0 }
    ///
    /// Facet format: "fieldName:value1,value2"
    /// Example: GET /api/v1/search?q=shoes&amp;facets=brand:Nike,Adidas&amp;facets=size:42
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string?              q          = null,
        [FromQuery] List<string>?        facets     = null,
        [FromQuery] decimal?             minPrice   = null,
        [FromQuery] decimal?             maxPrice   = null,
        [FromQuery] string?              sort       = null,
        [FromQuery] int                  page       = 1,
        [FromQuery] int                  pageSize   = 20,
        CancellationToken                ct         = default)
    {
        var request = new CognitiveSearchRequest
        {
            Query     = q,
            Facets    = facets?.AsReadOnly() ?? (IReadOnlyList<string>)[],
            MinPrice  = minPrice,
            MaxPrice  = maxPrice,
            Sort      = sort,
            Page      = Math.Max(1, page),
            PageSize  = Math.Clamp(pageSize, 1, 100),
        };

        var response = await cognitiveSearch.SearchAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// ENH-SRCH-002 / ENH-CAT-006 — Autocomplete suggestions.
    /// Uses Azure Cognitive Search Suggester when ACS is configured;
    /// falls back to DB-backed prefix search otherwise.
    /// Returns an empty suggestions array (HTTP 200) for queries shorter than 2 characters.
    /// </summary>
    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(
        [FromQuery] string q = "",
        CancellationToken ct = default)
    {
        // Route through cognitive search autocomplete (falls back to DB suggest internally)
        var suggestions = await cognitiveSearch.AutocompleteAsync(q, ct);
        return Ok(new { query = q, suggestions });
    }
}
