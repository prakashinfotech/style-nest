/**
 * ENH-PDP-003 — Size Guide Modal: brand-specific size charts with cm/inches support.
 *
 * A SizeGuide belongs to a Brand and optionally a Category (e.g. "Men's Tops", "Women's Dresses").
 * The chart data is stored as JSON in ChartJson:
 *
 *   {
 *     "columns": ["Size", "Chest (cm)", "Waist (cm)"],
 *     "rows":    [["XS","84-86","70-72"], ["S","88-92","74-78"]],
 *     "inchConversion": 0.393701
 *   }
 *
 * The front-end toggles between cm (raw) and inches (multiply each numeric segment by 0.393701).
 * inchConversion is stored so admin can override rounding conventions per guide.
 *
 * Uniqueness: one active guide per (BrandId, CategoryId?) — enforced via unique index.
 * If CategoryId is null the guide applies to all categories for that brand (fallback).
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class SizeGuide : BaseEntity<Guid>
{
    public Guid   BrandId     { get; set; }

    /// <summary>Optional category scope. Null = applies to all categories for this brand.</summary>
    public Guid?  CategoryId  { get; set; }

    /// <summary>Human-readable label, e.g. "Men's Tops &amp; T-Shirts".</summary>
    public string GuideName   { get; set; } = string.Empty;

    /// <summary>JSON chart payload — columns, rows (string[][] or mixed), inchConversion factor.</summary>
    public string ChartJson   { get; set; } = "{}";

    // Navigations
    public Brand     Brand    { get; set; } = null!;
    public Category? Category { get; set; }
}
