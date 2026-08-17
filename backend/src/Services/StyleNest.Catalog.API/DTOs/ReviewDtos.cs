namespace StyleNest.Catalog.API.DTOs;

public record ReviewDto(
    Guid   Id,
    Guid   ProductId,
    Guid   UserId,
    string Author,
    int    Rating,
    string Title,
    string Body,
    DateTime CreatedAt,
    /// <summary>ENH-PDP-008 — Up to 4 photo URLs attached to this review. Null when no photos.</summary>
    IReadOnlyList<string>? PhotoUrls = null
);

public record CreateReviewRequest(
    int    Rating,
    string Title,
    string Body,
    /// <summary>ENH-PDP-008 — Optional list of photo URLs (max 4, each max 500 chars).</summary>
    IReadOnlyList<string>? PhotoUrls = null
);
