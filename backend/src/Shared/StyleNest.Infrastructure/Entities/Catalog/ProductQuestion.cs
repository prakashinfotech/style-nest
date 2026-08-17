/**
 * ENH-PDP-004 — Q&A Section: product questions posted by shoppers.
 *
 * One product may have many questions; each question has 0..n answers.
 * Questions are approved by default (no pre-moderation) — admins can soft-delete abusive content.
 * Upvote count lives on ProductAnswer to surface the most-helpful answer.
 */

using StyleNest.Infrastructure.Entities.Commerce;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class ProductQuestion : BaseEntity<Guid>
{
    public Guid   ProductId  { get; set; }
    public Guid   UserId     { get; set; }

    /// <summary>The shopper's question text (max 500 chars).</summary>
    public string QuestionText { get; set; } = string.Empty;

    // Navigation
    public Product                      Product { get; set; } = null!;
    public ICollection<ProductAnswer>   Answers { get; set; } = new List<ProductAnswer>();
}
