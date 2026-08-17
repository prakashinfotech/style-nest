/**
 * ENH-SELL-002 — Seller KYC / Verification Workflow
 * Source: FR-SELL (TSD §5)
 *
 * Workflow:
 *   1. Seller submits one or more KYC documents (GST, PAN, bank statement, …).
 *   2. Each document starts as KycDocumentStatus.Pending.
 *   3. Admin reviews each document: Approve → KycDocumentStatus.Approved
 *                                   Reject  → KycDocumentStatus.Rejected (seller must re-submit)
 *   4. When ALL documents for a seller are Approved, an admin calls ActivateSellerAsync
 *      which transitions Seller.Status → Active and records Seller.ApprovedAt.
 *
 * Cross-tenant isolation: every query is scoped by SellerId / the caller's identity.
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.DTOs;

namespace StyleNest.Seller.API.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Request from a seller to submit a KYC document.</summary>
public sealed record SubmitKycDocumentRequest(
    KycDocumentType DocumentType,
    string          DocumentUrl);

/// <summary>Admin review decision for a KYC document.</summary>
public sealed record KycReviewRequest(
    bool    Approved,
    string? Note = null);

/// <summary>Read-only view of a single KYC document (safe for both seller and admin).</summary>
public sealed record KycDocumentDto(
    Guid              Id,
    Guid              SellerId,
    KycDocumentType   DocumentType,
    string            DocumentTypeDisplay,
    string            DocumentUrl,
    KycDocumentStatus Status,
    string            StatusDisplay,
    Guid?             ReviewedBy,
    DateTime?         ReviewedAt,
    string?           ReviewNote,
    DateTime          CreatedAt);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface ISellerKycService
{
    /// <summary>
    /// Seller submits a KYC document for admin review.
    /// Throws <see cref="KeyNotFoundException"/> when <paramref name="sellerId"/> does not exist.
    /// </summary>
    Task<KycDocumentDto> SubmitDocumentAsync(
        Guid sellerId, SubmitKycDocumentRequest request, CancellationToken ct = default);

    /// <summary>Returns all KYC documents submitted by <paramref name="sellerId"/>, newest first.</summary>
    Task<IReadOnlyList<KycDocumentDto>> GetDocumentsAsync(
        Guid sellerId, CancellationToken ct = default);

    /// <summary>Admin: returns all documents currently in Pending status across all sellers.</summary>
    Task<IReadOnlyList<KycDocumentDto>> GetPendingReviewsAsync(CancellationToken ct = default);

    /// <summary>
    /// Admin reviews (approves or rejects) a specific KYC document.
    /// Throws <see cref="KeyNotFoundException"/> when <paramref name="documentId"/> does not exist.
    /// </summary>
    Task<KycDocumentDto> ReviewDocumentAsync(
        Guid adminId, Guid documentId, KycReviewRequest request, CancellationToken ct = default);

    /// <summary>
    /// Admin activates a seller account after all KYC documents have been approved.
    /// <para>
    /// Throws <see cref="InvalidOperationException"/> when:
    /// <list type="bullet">
    ///   <item>no documents have been submitted;</item>
    ///   <item>one or more documents are not in <see cref="KycDocumentStatus.Approved"/> state.</item>
    /// </list>
    /// Throws <see cref="KeyNotFoundException"/> when <paramref name="sellerId"/> does not exist.
    /// </para>
    /// </summary>
    Task<SellerProfileDto> ActivateSellerAsync(
        Guid adminId, Guid sellerId, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class SellerKycService(AppDbContext db) : ISellerKycService
{
    public async Task<KycDocumentDto> SubmitDocumentAsync(
        Guid sellerId, SubmitKycDocumentRequest request, CancellationToken ct = default)
    {
        // Verify seller exists
        var sellerExists = await db.Sellers
            .AnyAsync(s => s.Id == sellerId && !s.IsDeleted, ct);

        if (!sellerExists)
            throw new KeyNotFoundException($"Seller {sellerId} not found.");

        var doc = new SellerKycDocument
        {
            Id           = Guid.NewGuid(),
            SellerId     = sellerId,
            DocumentType = request.DocumentType,
            DocumentUrl  = request.DocumentUrl,
            Status       = KycDocumentStatus.Pending,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };

        db.SellerKycDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        return Map(doc);
    }

    public async Task<IReadOnlyList<KycDocumentDto>> GetDocumentsAsync(
        Guid sellerId, CancellationToken ct = default)
    {
        var docs = await db.SellerKycDocuments
            .AsNoTracking()
            .Where(d => d.SellerId == sellerId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return docs.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<KycDocumentDto>> GetPendingReviewsAsync(
        CancellationToken ct = default)
    {
        var docs = await db.SellerKycDocuments
            .AsNoTracking()
            .Where(d => d.Status == KycDocumentStatus.Pending && !d.IsDeleted)
            .OrderBy(d => d.CreatedAt)   // oldest first — FIFO review queue
            .ToListAsync(ct);

        return docs.Select(Map).ToList();
    }

    public async Task<KycDocumentDto> ReviewDocumentAsync(
        Guid adminId, Guid documentId, KycReviewRequest request, CancellationToken ct = default)
    {
        var doc = await db.SellerKycDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, ct)
            ?? throw new KeyNotFoundException($"KYC document {documentId} not found.");

        doc.Status     = request.Approved ? KycDocumentStatus.Approved : KycDocumentStatus.Rejected;
        doc.ReviewedBy = adminId;
        doc.ReviewedAt = DateTime.UtcNow;
        doc.ReviewNote = request.Note;
        doc.UpdatedAt  = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Map(doc);
    }

    public async Task<SellerProfileDto> ActivateSellerAsync(
        Guid adminId, Guid sellerId, CancellationToken ct = default)
    {
        var seller = await db.Sellers
            .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted, ct)
            ?? throw new KeyNotFoundException($"Seller {sellerId} not found.");

        var docs = await db.SellerKycDocuments
            .AsNoTracking()
            .Where(d => d.SellerId == sellerId && !d.IsDeleted)
            .ToListAsync(ct);

        if (docs.Count == 0)
            throw new InvalidOperationException(
                "Cannot activate seller: no KYC documents have been submitted.");

        var notApproved = docs.Where(d => d.Status != KycDocumentStatus.Approved).ToList();
        if (notApproved.Count > 0)
            throw new InvalidOperationException(
                $"Cannot activate seller: {notApproved.Count} document(s) are not yet approved. "
              + "All submitted documents must be approved before activation.");

        seller.Status     = SellerStatus.Active;
        seller.ApprovedAt = DateTime.UtcNow;
        seller.UpdatedAt  = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new SellerProfileDto(
            seller.Id,
            seller.StoreName,
            seller.Slug,
            seller.Description,
            seller.LogoUrl,
            seller.GstNumber,
            seller.PanNumber,
            seller.Status.ToString(),
            seller.CommissionRate,
            seller.ApprovedAt,
            seller.CreatedAt);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static KycDocumentDto Map(SellerKycDocument d) =>
        new(d.Id,
            d.SellerId,
            d.DocumentType,
            DocumentTypeDisplay(d.DocumentType),
            d.DocumentUrl,
            d.Status,
            d.Status.ToString(),
            d.ReviewedBy,
            d.ReviewedAt,
            d.ReviewNote,
            d.CreatedAt);

    private static string DocumentTypeDisplay(KycDocumentType t) => t switch
    {
        KycDocumentType.GstCertificate => "GST Certificate",
        KycDocumentType.PanCard        => "PAN Card",
        KycDocumentType.BankStatement  => "Bank Statement",
        KycDocumentType.AadharCard     => "Aadhaar Card",
        KycDocumentType.Other          => "Other Document",
        _                              => t.ToString(),
    };
}
