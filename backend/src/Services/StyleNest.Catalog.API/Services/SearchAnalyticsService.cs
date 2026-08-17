using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Analytics;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

/// <summary>
/// ENH-SRCH-004 — Search Analytics: records every distinct search term (upsert by Term),
/// increments Count on each search, and separately tracks ZeroResultCount for terms that
/// returned no products.
///
/// Design notes:
/// - The upsert is done using EF Core ExecuteUpdateAsync which avoids a SELECT + INSERT/UPDATE race.
/// - Falls back to INSERT when the term is new (first-time upsert path).
/// - Logging-only on failure so a write error never propagates back to the user.
/// - Called fire-and-forget via Task.Run from CatalogService to avoid adding latency
///   to the main search response path (analytics writes are eventually consistent).
/// </summary>
public interface ISearchAnalyticsService
{
    /// <summary>Records a search event for the given term.  <paramref name="hasResults"/> = false → also increments ZeroResultCount.</summary>
    Task RecordSearchAsync(string term, bool hasResults, CancellationToken ct = default);

    /// <summary>Returns the top N most-searched terms, ordered by Count descending.</summary>
    Task<IReadOnlyList<SearchTermDto>> GetTopTermsAsync(int take = 20, CancellationToken ct = default);

    /// <summary>Returns the top N zero-result terms, ordered by ZeroResultCount descending.</summary>
    Task<IReadOnlyList<SearchTermDto>> GetZeroResultTermsAsync(int take = 20, CancellationToken ct = default);
}

public sealed record SearchTermDto(string Term, int Count, int ZeroResultCount, DateTime LastSearchedAt);

public sealed class SearchAnalyticsService(
    AppDbContext db,
    ILogger<SearchAnalyticsService> logger) : ISearchAnalyticsService
{
    public async Task RecordSearchAsync(string term, bool hasResults, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        var normalised = term.Trim().ToLowerInvariant();
        if (normalised.Length > 200) normalised = normalised[..200]; // DB column guard

        try
        {
            var updated = await db.SearchTerms
                .Where(s => s.Term == normalised)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.Count, s => s.Count + 1)
                    .SetProperty(s => s.ZeroResultCount,
                        s => hasResults ? s.ZeroResultCount : s.ZeroResultCount + 1)
                    .SetProperty(s => s.LastSearchedAt, _ => DateTime.UtcNow),
                ct);

            if (updated == 0)
            {
                // First time this term has been searched — insert a new row
                db.SearchTerms.Add(new SearchTerm
                {
                    Id              = Guid.NewGuid(),
                    Term            = normalised,
                    Count           = 1,
                    ZeroResultCount = hasResults ? 0 : 1,
                    LastSearchedAt  = DateTime.UtcNow,
                });
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            // Analytics writes must never break the main response path
            logger.LogWarning(ex, "ENH-SRCH-004 SearchAnalyticsService.RecordSearchAsync failed for term '{Term}'", normalised);
        }
    }

    public async Task<IReadOnlyList<SearchTermDto>> GetTopTermsAsync(int take = 20, CancellationToken ct = default)
    {
        return await db.SearchTerms
            .AsNoTracking()
            .OrderByDescending(s => s.Count)
            .Take(take)
            .Select(s => new SearchTermDto(s.Term, s.Count, s.ZeroResultCount, s.LastSearchedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SearchTermDto>> GetZeroResultTermsAsync(int take = 20, CancellationToken ct = default)
    {
        return await db.SearchTerms
            .AsNoTracking()
            .Where(s => s.ZeroResultCount > 0)
            .OrderByDescending(s => s.ZeroResultCount)
            .Take(take)
            .Select(s => new SearchTermDto(s.Term, s.Count, s.ZeroResultCount, s.LastSearchedAt))
            .ToListAsync(ct);
    }
}
