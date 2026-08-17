/**
 * ENH-SRCH-002 — Search Autocomplete + Typeahead (≤200ms p95)
 * Acceptance criteria:
 *   - Returns array of suggestion strings for q ≥ 2 characters
 *   - q < 2 chars → empty array (not an error)
 *   - Suggestions sourced from: popular SearchTerms, Products, Categories, Brands
 *   - Prefix match, case-insensitive, top 10 max
 *   - All queries use AsNoTracking + StartsWith (EF → LIKE 'q%') for speed
 */

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── Response ──────────────────────────────────────────────────────────────────

/// <summary>ENH-SRCH-002 — Autocomplete response.</summary>
public sealed record SearchSuggestResponse(
    string         Query,
    List<string>   Suggestions);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface ISearchSuggestService
{
    /// <summary>
    /// ENH-SRCH-002 — Returns up to 10 autocomplete suggestions for the given query.
    /// Returns an empty list when <paramref name="query"/> has fewer than 2 characters.
    /// Never throws; returns empty list on any error.
    /// </summary>
    Task<SearchSuggestResponse> GetSuggestionsAsync(string query, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// ENH-SRCH-002 — Autocomplete backed by EF Core prefix queries across four sources:
/// 1. Popular <see cref="StyleNest.Infrastructure.Entities.Analytics.SearchTerm"/> records (ordered by Count desc)
/// 2. Product names
/// 3. Category names
/// 4. Brand names
///
/// All queries use <c>AsNoTracking</c> + <c>StartsWith</c> (translates to SQL <c>LIKE 'q%'</c>)
/// and are capped at 5 items each before deduplication, returning max 10 suggestions total.
/// </summary>
public sealed class SearchSuggestService(
    AppDbContext db,
    ILogger<SearchSuggestService> logger) : ISearchSuggestService
{
    private const int MinQueryLength  = 2;
    private const int MaxSuggestions  = 10;
    private const int SourceLimit     = 5;   // items fetched per source before merge

    public async Task<SearchSuggestResponse> GetSuggestionsAsync(
        string query, CancellationToken ct = default)
    {
        query = (query ?? string.Empty).Trim();

        if (query.Length < MinQueryLength)
            return new SearchSuggestResponse(query, []);

        try
        {
            // Normalise to lower-case so LIKE is always case-insensitive in both
            // SQL Server (default CI collation) and the in-memory EF Core provider.
            var qLower = query.ToLowerInvariant();

            // Source 1: popular search terms (highest Count first)
            var terms = await db.SearchTerms
                .AsNoTracking()
                .Where(t => t.Term.ToLower().StartsWith(qLower))
                .OrderByDescending(t => t.Count)
                .Take(SourceLimit)
                .Select(t => t.Term)
                .ToListAsync(ct);

            // Source 2: product names
            var products = await db.Products
                .AsNoTracking()
                .Where(p => p.Name.ToLower().StartsWith(qLower))
                .Take(SourceLimit)
                .Select(p => p.Name)
                .ToListAsync(ct);

            // Source 3: category names
            var categories = await db.Categories
                .AsNoTracking()
                .Where(c => c.Name.ToLower().StartsWith(qLower))
                .Take(SourceLimit)
                .Select(c => c.Name)
                .ToListAsync(ct);

            // Source 4: brand names
            var brands = await db.Brands
                .AsNoTracking()
                .Where(b => b.Name.ToLower().StartsWith(qLower))
                .Take(SourceLimit)
                .Select(b => b.Name)
                .ToListAsync(ct);

            // Merge: popular terms first, then products, categories, brands
            // Deduplicate case-insensitively, cap at MaxSuggestions
            var suggestions = terms
                .Concat(products)
                .Concat(categories)
                .Concat(brands)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxSuggestions)
                .ToList();

            // Source 5: ENH-SRCH-003 — synonym expansion.
            // If the exact query term matches a synonym entry, append the synonym
            // values so users discover related terms (e.g. "tee" → ["t-shirt","polo"]).
            var synonymEntry = await db.SearchSynonyms
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Term == qLower, ct);

            if (synonymEntry is not null)
            {
                var synonymTerms = JsonSerializer.Deserialize<string[]>(synonymEntry.SynonymsJson)
                                   ?? [];
                suggestions = suggestions
                    .Concat(synonymTerms)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaxSuggestions)
                    .ToList();
            }

            return new SearchSuggestResponse(query, suggestions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchSuggest: error for query '{Query}' — returning empty", query);
            return new SearchSuggestResponse(query, []);
        }
    }
}
