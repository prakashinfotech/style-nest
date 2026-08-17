/**
 * ENH-SELL-002 — Seller KYC / Verification Workflow
 * Source: FR-SELL (TSD §5)
 *
 * A KYC document submitted by a seller for admin review.
 * Workflow: Pending → UnderReview → Approved | Rejected
 * Once ALL documents for a seller are Approved, an admin can activate the seller account.
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Seller;

/// <summary>Types of identity/compliance documents accepted during KYC.</summary>
public enum KycDocumentType
{
    GstCertificate  = 1,
    PanCard         = 2,
    BankStatement   = 3,
    AadharCard      = 4,
    Other           = 5,
}

/// <summary>Review lifecycle of a single KYC document submission.</summary>
public enum KycDocumentStatus
{
    /// <summary>Submitted by seller, awaiting admin attention.</summary>
    Pending     = 0,
    /// <summary>Admin has opened the document for review (optional intermediate state).</summary>
    UnderReview = 1,
    /// <summary>Document accepted by admin.</summary>
    Approved    = 2,
    /// <summary>Document rejected by admin — seller must re-submit.</summary>
    Rejected    = 3,
}

/// <summary>
/// ENH-SELL-002 — A single KYC document submitted by a seller.
/// The document URL points to an object-store asset (e.g. MinIO / Azure Blob);
/// the raw file is never stored in the database.
/// </summary>
public class SellerKycDocument : BaseEntity<Guid>
{
    /// <summary>The seller who submitted this document.</summary>
    public Guid SellerId { get; set; }

    /// <summary>Type of the document (GST, PAN, bank statement, etc.).</summary>
    public KycDocumentType DocumentType { get; set; }

    /// <summary>URL of the uploaded document in object storage.</summary>
    public string DocumentUrl { get; set; } = string.Empty;

    /// <summary>Current review status. Defaults to <see cref="KycDocumentStatus.Pending"/>.</summary>
    public KycDocumentStatus Status { get; set; } = KycDocumentStatus.Pending;

    /// <summary>UserId of the admin who reviewed this document (null until reviewed).</summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>UTC timestamp when the admin completed the review (null until reviewed).</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Admin note: approval confirmation text or rejection reason.</summary>
    public string? ReviewNote { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public Seller Seller { get; set; } = null!;
}
