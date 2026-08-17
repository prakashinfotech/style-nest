/**
 * ENH-SRCH-003 — Search Synonyms Dictionary.
 *
 * Each row maps a canonical trigger term to a JSON array of synonym strings.
 * Example: { Term: "tee", SynonymsJson: ["t-shirt","polo","top"] }
 *
 * Terms are stored normalised (lowercase, trimmed) so lookups are case-insensitive
 * without requiring a collation-aware index.
 *
 * The SynonymsJson payload is a JSON string array — kept flexible so admins
 * can manage the dictionary via the Admin CMS without schema changes.
 *
 * When a user's search query exactly matches a Term, SearchSuggestService
 * appends the synonym values to the autocomplete suggestions.
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Analytics;

public class SearchSynonym : BaseEntity<Guid>
{
    /// <summary>Normalised trigger term (lowercase, trimmed). Unique across active rows.</summary>
    public string Term { get; set; } = string.Empty;

    /// <summary>JSON array of synonym strings, e.g. ["t-shirt","polo","top"].</summary>
    public string SynonymsJson { get; set; } = "[]";
}
