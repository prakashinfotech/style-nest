/**
 * ENH-PDP-004 — Q&A Section: answers to product questions.
 *
 * Answers may be posted by:
 *   - Other shoppers (AnswererRole = "Shopper")
 *   - The seller (AnswererRole = "Seller")
 *   - An admin (AnswererRole = "Admin")
 *
 * UpvoteCount is incremented/decremented via a dedicated endpoint (no separate upvote entity
 * for simplicity — exact de-dup is out of scope for this sprint).
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class ProductAnswer : BaseEntity<Guid>
{
    public Guid   QuestionId   { get; set; }
    public Guid   AnswererId   { get; set; }

    /// <summary>"Shopper" | "Seller" | "Admin"</summary>
    public string AnswererRole { get; set; } = "Shopper";

    /// <summary>Answer body (max 1000 chars).</summary>
    public string AnswerText   { get; set; } = string.Empty;

    /// <summary>Cumulative upvotes received (non-negative).</summary>
    public int    UpvoteCount  { get; set; }

    // Navigation
    public ProductQuestion Question { get; set; } = null!;
}
