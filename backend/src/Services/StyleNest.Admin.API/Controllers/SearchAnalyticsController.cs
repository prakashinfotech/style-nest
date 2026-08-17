/**
 * ENH-SRCH-004 — Search Analytics admin endpoints.
 *
 * GET api/v1/admin/analytics/search/top-terms     → top N searched terms by frequency
 * GET api/v1/admin/analytics/search/zero-results  → terms that returned 0 products
 *
 * All endpoints require Admin or SuperAdmin role.
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/analytics/search")]
[Authorize(Roles = "Admin,SuperAdmin")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class SearchAnalyticsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns the top searched terms ordered by total search count descending.
    /// </summary>
    /// <param name="take">Maximum number of terms to return (1–100, default 20).</param>
    [HttpGet("top-terms")]
    [ProducesResponseType(typeof(IReadOnlyList<SearchTermAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopTerms([FromQuery] int take = 20, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var terms = await db.SearchTerms
            .AsNoTracking()
            .OrderByDescending(s => s.Count)
            .Take(take)
            .Select(s => new SearchTermAnalyticsDto(s.Term, s.Count, s.ZeroResultCount, s.LastSearchedAt))
            .ToListAsync(ct);
        return Ok(terms);
    }

    /// <summary>
    /// Returns terms that returned zero results, ordered by zero-result count descending.
    /// These are candidates for synonym creation (ENH-SRCH-003) or catalog gaps.
    /// </summary>
    /// <param name="take">Maximum number of terms to return (1–100, default 20).</param>
    [HttpGet("zero-results")]
    [ProducesResponseType(typeof(IReadOnlyList<SearchTermAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetZeroResultTerms([FromQuery] int take = 20, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var terms = await db.SearchTerms
            .AsNoTracking()
            .Where(s => s.ZeroResultCount > 0)
            .OrderByDescending(s => s.ZeroResultCount)
            .Take(take)
            .Select(s => new SearchTermAnalyticsDto(s.Term, s.Count, s.ZeroResultCount, s.LastSearchedAt))
            .ToListAsync(ct);
        return Ok(terms);
    }
}

/// <summary>ENH-SRCH-004 — DTO returned by search analytics endpoints.</summary>
public sealed record SearchTermAnalyticsDto(
    string   Term,
    int      TotalSearches,
    int      ZeroResultSearches,
    DateTime LastSearchedAt);
