using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Analytics;

public class SearchTerm : BaseEntity<Guid>
{
    public string Term { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public DateTime LastSearchedAt { get; set; }
    /// <summary>ENH-SRCH-004 — Number of times this term returned zero results.</summary>
    public int ZeroResultCount { get; set; } = 0;
}
