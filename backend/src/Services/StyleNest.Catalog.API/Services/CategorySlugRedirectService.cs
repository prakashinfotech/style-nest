/**
 * ENH-CAT-008 — Category Slug 301-Redirect on Rename (EC-CAT-003)
 *
 * When a category is renamed its old slug is recorded in CategorySlugHistory.
 * Clients that visit the old URL (e.g. Google with cached pages, or bookmarks)
 * must receive an HTTP 301 Moved Permanently so link equity is preserved.
 *
 * This service provides:
 *   ResolveAsync(slug) — given any slug:
 *     • If it matches a live category → returns (category, needsRedirect=false)
 *     • If it matches a history entry  → follows the chain and returns (category, needsRedirect=true, currentSlug)
 *     • If no match at all             → returns null (caller should 404)
 *
 * Redirect chain safety: walks at most MaxRedirectDepth steps to prevent
 * infinite loops in case of circular rename data.
 *
 * Acceptance criteria (TC-CAT-008-*):
 *   TC-CAT-008-01: Current slug → resolves to category, needsRedirect=false
 *   TC-CAT-008-02: Old slug (single rename) → needsRedirect=true, currentSlug=new
 *   TC-CAT-008-03: Old slug (chain A→B→C) → resolves to C
 *   TC-CAT-008-04: Unknown slug → returns null
 *   TC-CAT-008-05: RenameCategoryAsync records slug history
 *   TC-CAT-008-06: RenameCategoryAsync updates Category.Slug
 *   TC-CAT-008-07: Renaming to same slug → no history row created (no-op)
 *   TC-CAT-008-08: GetCategoryBySlugAsync controller integration — 200 for current, 301 for old
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── Result type ───────────────────────────────────────────────────────────────

/// <summary>ENH-CAT-008 — Result of a slug resolution attempt.</summary>
public sealed record SlugResolution(
    Category? Category,
    bool      NeedsRedirect,
    string?   CurrentSlug);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface ICategorySlugRedirectService
{
    /// <summary>
    /// ENH-CAT-008 — Resolves any slug (current or old) to a category.
    /// Returns <c>null</c> if the slug is completely unknown.
    /// When <see cref="SlugResolution.NeedsRedirect"/> is true the caller should
    /// issue an HTTP 301 to <see cref="SlugResolution.CurrentSlug"/>.
    /// </summary>
    Task<SlugResolution?> ResolveAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// ENH-CAT-008 — Renames a category: updates its name and slug, and records
    /// the old slug in <c>CategorySlugHistory</c> so old URLs 301-redirect.
    /// If <paramref name="newName"/> produces the same slug as the current one,
    /// only the display name is updated and no history row is written.
    /// </summary>
    Task<Category?> RenameCategoryAsync(
        Guid categoryId, string newName, DateTime? now = null, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>ENH-CAT-008 — Category slug redirect resolver.</summary>
public sealed class CategorySlugRedirectService(
    AppDbContext db,
    ILogger<CategorySlugRedirectService> logger) : ICategorySlugRedirectService
{
    /// <summary>Maximum redirect hops to follow (prevents infinite loops).</summary>
    public const int MaxRedirectDepth = 10;

    public async Task<SlugResolution?> ResolveAsync(
        string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        // ── Step 1: is this already the current slug? ─────────────────────────
        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);

        if (category is not null)
            return new SlugResolution(category, NeedsRedirect: false, CurrentSlug: slug);

        // ── Step 2: follow the slug-history chain ─────────────────────────────
        var currentSlug = slug;

        for (int depth = 0; depth < MaxRedirectDepth; depth++)
        {
            var history = await db.CategorySlugHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.OldSlug == currentSlug, ct);

            if (history is null)
                break;          // chain end — no match found

            currentSlug = history.NewSlug;

            // Check whether currentSlug is now a live category slug
            category = await db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == currentSlug, ct);

            if (category is not null)
            {
                logger.LogDebug(
                    "{EventType} OldSlug={OldSlug} CurrentSlug={CurrentSlug} Depth={Depth}",
                    "CATEGORY_SLUG_REDIRECT", slug, currentSlug, depth + 1);

                return new SlugResolution(category, NeedsRedirect: true, CurrentSlug: currentSlug);
            }
        }

        return null;   // slug is completely unknown or chain is broken
    }

    public async Task<Category?> RenameCategoryAsync(
        Guid categoryId, string newName, DateTime? now = null, CancellationToken ct = default)
    {
        var renamedAt = now ?? DateTime.UtcNow;
        var newSlug   = GenerateSlug(newName);

        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
            return null;

        var oldSlug = category.Slug;

        // ── No-op if slug unchanged ───────────────────────────────────────────
        if (string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase))
        {
            // Only update the display name, no redirect record needed
            category.Name      = newName;
            category.UpdatedAt = renamedAt;
            await db.SaveChangesAsync(ct);
            return category;
        }

        // ── Record old slug in history ────────────────────────────────────────
        db.CategorySlugHistories.Add(new CategorySlugHistory
        {
            Id         = Guid.NewGuid(),
            CategoryId = categoryId,
            OldSlug    = oldSlug,
            NewSlug    = newSlug,
            ReplacedAt = renamedAt,
            CreatedAt  = renamedAt,
            UpdatedAt  = renamedAt,
        });

        // ── Apply rename ──────────────────────────────────────────────────────
        category.Name      = newName;
        category.Slug      = newSlug;
        category.UpdatedAt = renamedAt;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "{EventType} CategoryId={CategoryId} OldSlug={OldSlug} NewSlug={NewSlug}",
            "CATEGORY_RENAMED", categoryId, oldSlug, newSlug);

        return category;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string GenerateSlug(string name) =>
        name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("&", "and")
            .Replace("'", string.Empty);
}
