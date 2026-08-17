/**
 * ENH-SRCH-003 — Search Synonyms Dictionary: Admin CMS service.
 *
 * Business rules (FR-SRCH, TSD §7.1):
 *   - Terms are normalised to lowercase+trimmed on create/update so lookups are
 *     case-insensitive without a collation-aware index.
 *   - UpsertAsync: create or update (Term is the natural key); revives soft-deleted rows.
 *   - DeleteAsync: soft-delete; idempotent (no-throw when not found).
 *   - ExpandAsync: returns the synonym list for a given term; empty when term unknown.
 *   - GetAllAsync: returns all active synonyms ordered alphabetically.
 */

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StyleNest.Infrastructure.Entities.Analytics;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Admin.API.Services;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record SearchSynonymDto(
    Guid     Id,
    string   Term,
    string[] Synonyms,
    DateTime UpdatedAt);

public sealed record UpsertSynonymRequest(
    string   Term,
    string[] Synonyms);

// ─── Interface ────────────────────────────────────────────────────────────────

public interface ISearchSynonymService
{
    /// <summary>Returns all active synonym entries ordered alphabetically by term.</summary>
    Task<IReadOnlyList<SearchSynonymDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Creates or updates the synonym entry for the given term.</summary>
    Task<SearchSynonymDto> UpsertAsync(UpsertSynonymRequest req, CancellationToken ct = default);

    /// <summary>Soft-deletes the synonym entry by id. Idempotent.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the synonym strings for the given term (case-insensitive lookup).
    /// Returns an empty list when no entry exists.
    /// </summary>
    Task<IReadOnlyList<string>> ExpandAsync(string term, CancellationToken ct = default);
}

// ─── Implementation ──────────────────────────────────────────────────────────

public sealed class SearchSynonymService(AppDbContext db) : ISearchSynonymService
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchSynonymDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.SearchSynonyms
            .AsNoTracking()
            .OrderBy(s => s.Term)
            .ToListAsync(ct);

        return rows.Select(MapDto).ToList();
    }

    /// <inheritdoc />
    public async Task<SearchSynonymDto> UpsertAsync(
        UpsertSynonymRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Term))
            throw new ArgumentException("Term must not be empty.", nameof(req));

        if (req.Synonyms is null || req.Synonyms.Length == 0)
            throw new ArgumentException("Synonyms must contain at least one entry.", nameof(req));

        var normalisedTerm = req.Term.Trim().ToLowerInvariant();
        var synonymsJson   = JsonSerializer.Serialize(req.Synonyms);
        var now            = DateTime.UtcNow;

        // IgnoreQueryFilters allows detecting and reviving soft-deleted rows.
        var existing = await db.SearchSynonyms
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Term == normalisedTerm, ct);

        if (existing is not null)
        {
            existing.SynonymsJson = synonymsJson;
            existing.IsDeleted    = false; // revive if previously soft-deleted
            existing.UpdatedAt    = now;
        }
        else
        {
            existing = new SearchSynonym
            {
                Id           = Guid.NewGuid(),
                Term         = normalisedTerm,
                SynonymsJson = synonymsJson,
                CreatedAt    = now,
                UpdatedAt    = now,
            };
            db.SearchSynonyms.Add(existing);
        }

        await db.SaveChangesAsync(ct);
        return MapDto(existing);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await db.SearchSynonyms
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (entry is null) return; // idempotent

        entry.IsDeleted = true;
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ExpandAsync(
        string term, CancellationToken ct = default)
    {
        var normalisedTerm = (term ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalisedTerm)) return [];

        var entry = await db.SearchSynonyms
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Term == normalisedTerm, ct);

        if (entry is null) return [];

        return JsonSerializer.Deserialize<string[]>(entry.SynonymsJson, _jsonOpts) ?? [];
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static SearchSynonymDto MapDto(SearchSynonym s)
    {
        var synonyms = JsonSerializer.Deserialize<string[]>(s.SynonymsJson, _jsonOpts) ?? [];
        return new SearchSynonymDto(s.Id, s.Term, synonyms, s.UpdatedAt);
    }
}
