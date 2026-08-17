/**
 * ENH-CAT-008 — Category Slug 301-Redirect on Rename (EC-CAT-003)
 * One row is written here each time a category's slug changes.
 * The redirect service walks this table to resolve any old slug to the
 * most-current slug so a 301 can be issued to clients and search engines.
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

/// <summary>
/// ENH-CAT-008 — Immutable slug-change record.
/// One row per rename event: (CategoryId, OldSlug, NewSlug, ReplacedAt).
/// </summary>
public class CategorySlugHistory : BaseEntity<Guid>
{
    public Guid     CategoryId  { get; set; }
    /// <summary>The slug that was active before this rename event.</summary>
    public string   OldSlug     { get; set; } = string.Empty;
    /// <summary>The slug that became active after this rename event.</summary>
    public string   NewSlug     { get; set; } = string.Empty;
    /// <summary>UTC timestamp when the rename took effect.</summary>
    public DateTime ReplacedAt  { get; set; }

    public Category Category    { get; set; } = null!;
}
