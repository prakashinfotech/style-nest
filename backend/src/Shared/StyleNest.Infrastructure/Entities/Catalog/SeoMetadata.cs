/**
 * ENH-CAT-007 — SEO Canonicalisation
 * Per-entity SEO override stored in the database.
 * When present, the stored values take priority over auto-generated templates.
 * Missing fields fall back to the template-generated value.
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

/// <summary>
/// ENH-CAT-007 — Optional SEO override for a product, category or brand.
/// Uniquely keyed on (EntityType, EntityId).
/// </summary>
public class SeoMetadata : BaseEntity<Guid>
{
    /// <summary>"product" | "category" | "brand"</summary>
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    /// <summary>Override for the HTML &lt;title&gt; tag. Null → auto-generated template.</summary>
    public string? TitleOverride { get; set; }

    /// <summary>Override for &lt;meta name="description"&gt;. Null → auto-generated template.</summary>
    public string? MetaDescriptionOverride { get; set; }

    /// <summary>
    /// Override for &lt;link rel="canonical"&gt;.
    /// Relative path (e.g. "/products/nike-air-max-90").
    /// Null → auto-generated from entity slug.
    /// </summary>
    public string? CanonicalPathOverride { get; set; }
}
