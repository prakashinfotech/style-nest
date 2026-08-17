using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class Review : BaseEntity<Guid>
{
    public Guid   ProductId { get; set; }
    public Guid   UserId    { get; set; }
    public int    Rating    { get; set; }   // 1–5
    public string Title     { get; set; } = string.Empty;
    public string Body      { get; set; } = string.Empty;
    public string Author    { get; set; } = string.Empty;

    /// <summary>
    /// ENH-PDP-008 — JSON array of up to 4 photo URLs submitted with the review.
    /// Example: ["https://cdn.example.com/review-1a.jpg","https://cdn.example.com/review-1b.jpg"]
    /// Stored as nvarchar(max) to avoid URL-length constraints.
    /// </summary>
    public string PhotoUrlsJson { get; set; } = "[]";

    public Product Product  { get; set; } = null!;
}
