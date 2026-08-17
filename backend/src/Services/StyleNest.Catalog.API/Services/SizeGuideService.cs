/**
 * ENH-PDP-003 — Size Guide Modal service.
 *
 * Business rules (FR-PDP-005):
 *   - Size guides are brand-specific; a category-scoped guide overrides the brand-wide fallback.
 *   - GetAsync: if a (brandId, categoryId) guide exists → return it;
 *               else try (brandId, null) as fallback; else null.
 *   - UpsertAsync: create or update the guide for (brandId, categoryId?).
 *   - DeleteAsync: soft-delete; subsequent calls to GetAsync will return null.
 *   - ChartJson: free-form JSON (client controls format); service validates it is valid JSON
 *     and contains at minimum a non-empty "columns" array.
 *
 * cm/inches toggle:
 *   The JSON payload includes an optional inchConversion factor (default 0.393701).
 *   The Angular component performs client-side multiplication — no server involvement.
 */

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public sealed record SizeGuideDto(
    Guid      Id,
    Guid      BrandId,
    Guid?     CategoryId,
    string    GuideName,
    string    ChartJson,
    DateTime  UpdatedAt);

public sealed record UpsertSizeGuideRequest(
    string    GuideName,
    string    ChartJson,
    Guid?     CategoryId = null);

// ─── Interface ───────────────────────────────────────────────────────────────

public interface ISizeGuideService
{
    /// <summary>
    /// Returns the size guide for a brand + optional category.
    /// Falls back to the brand-wide guide (CategoryId = null) when no category-specific guide exists.
    /// </summary>
    Task<SizeGuideDto?> GetAsync(Guid brandId, Guid? categoryId = null, CancellationToken ct = default);

    /// <summary>Create or update the size guide for (brandId, categoryId?).</summary>
    Task<SizeGuideDto> UpsertAsync(Guid brandId, UpsertSizeGuideRequest req, CancellationToken ct = default);

    /// <summary>Soft-delete the size guide for (brandId, categoryId?).</summary>
    Task DeleteAsync(Guid brandId, Guid? categoryId = null, CancellationToken ct = default);
}

// ─── Implementation ──────────────────────────────────────────────────────────

public sealed class SizeGuideService(AppDbContext db) : ISizeGuideService
{
    /// <inheritdoc />
    public async Task<SizeGuideDto?> GetAsync(
        Guid brandId, Guid? categoryId = null, CancellationToken ct = default)
    {
        // 1. Try exact match (brand + category)
        if (categoryId.HasValue)
        {
            var specific = await db.SizeGuides
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.BrandId == brandId && g.CategoryId == categoryId, ct);
            if (specific is not null) return MapDto(specific);
        }

        // 2. Fallback to brand-wide guide (CategoryId = null)
        var brandWide = await db.SizeGuides
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.BrandId == brandId && g.CategoryId == null, ct);

        return brandWide is null ? null : MapDto(brandWide);
    }

    /// <inheritdoc />
    public async Task<SizeGuideDto> UpsertAsync(
        Guid brandId, UpsertSizeGuideRequest req, CancellationToken ct = default)
    {
        ValidateChartJson(req.ChartJson);

        // IgnoreQueryFilters so we can revive soft-deleted rows
        var existing = await db.SizeGuides
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.BrandId == brandId && g.CategoryId == req.CategoryId, ct);

        var now = DateTime.UtcNow;

        if (existing is not null)
        {
            existing.GuideName  = req.GuideName;
            existing.ChartJson  = req.ChartJson;
            existing.IsDeleted  = false; // revive if previously deleted
            existing.UpdatedAt  = now;
        }
        else
        {
            existing = new SizeGuide
            {
                Id         = Guid.NewGuid(),
                BrandId    = brandId,
                CategoryId = req.CategoryId,
                GuideName  = req.GuideName,
                ChartJson  = req.ChartJson,
                CreatedAt  = now,
                UpdatedAt  = now,
            };
            db.SizeGuides.Add(existing);
        }

        await db.SaveChangesAsync(ct);
        return MapDto(existing);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid brandId, Guid? categoryId = null, CancellationToken ct = default)
    {
        var guide = await db.SizeGuides
            .FirstOrDefaultAsync(g => g.BrandId == brandId && g.CategoryId == categoryId, ct);

        if (guide is null) return; // idempotent

        guide.IsDeleted = true;
        guide.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static SizeGuideDto MapDto(SizeGuide g) =>
        new(g.Id, g.BrandId, g.CategoryId, g.GuideName, g.ChartJson, g.UpdatedAt);

    /// <summary>Ensures ChartJson is valid JSON with a non-empty "columns" array.</summary>
    private static void ValidateChartJson(string chartJson)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(chartJson);
        }
        catch (JsonException)
        {
            throw new ArgumentException("ChartJson must be valid JSON.", nameof(chartJson));
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("columns", out var cols) ||
                cols.ValueKind != JsonValueKind.Array ||
                cols.GetArrayLength() == 0)
            {
                throw new ArgumentException(
                    "ChartJson must contain a non-empty 'columns' array.", nameof(chartJson));
            }
        }
    }
}
