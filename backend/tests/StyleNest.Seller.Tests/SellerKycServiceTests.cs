/**
 * ENH-SELL-002 — Seller KYC / Verification Workflow
 * Source: FR-SELL (TSD §5)
 *
 * Acceptance criteria tested here:
 *
 *   TC-SELL-KYC-001: SubmitDocument → unknown sellerId → KeyNotFoundException
 *   TC-SELL-KYC-002: SubmitDocument → valid seller → persists with Status=Pending
 *   TC-SELL-KYC-003: SubmitDocument → correct SellerId, DocumentType, DocumentUrl in persisted row
 *   TC-SELL-KYC-004: GetDocuments → returns only the requesting seller's documents (cross-tenant)
 *   TC-SELL-KYC-005: GetDocuments → multiple docs ordered newest first
 *   TC-SELL-KYC-006: GetPendingReviews → returns all pending docs across all sellers
 *   TC-SELL-KYC-007: GetPendingReviews → does not return Approved or Rejected docs
 *   TC-SELL-KYC-008: ReviewDocument → Approve → Status=Approved, ReviewedBy, ReviewedAt populated
 *   TC-SELL-KYC-009: ReviewDocument → Reject → Status=Rejected, ReviewNote set
 *   TC-SELL-KYC-010: ReviewDocument → unknown documentId → KeyNotFoundException
 *   TC-SELL-KYC-011: ActivateSeller → no docs submitted → InvalidOperationException
 *   TC-SELL-KYC-012: ActivateSeller → some docs not Approved → InvalidOperationException
 *   TC-SELL-KYC-013: ActivateSeller → all docs Approved → Seller.Status=Active, ApprovedAt set
 *   TC-SELL-KYC-014: ActivateSeller → unknown sellerId → KeyNotFoundException
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.Services;
using Xunit;
using SellerEntity = StyleNest.Infrastructure.Entities.Seller.Seller;

namespace StyleNest.Seller.Tests;

public sealed class SellerKycServiceTests : IDisposable
{
    private readonly AppDbContext    _db;
    private readonly SellerKycService _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly DateTime T0  = new(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

    public SellerKycServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new SellerKycService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task<SellerEntity> SeedSellerAsync(
        SellerStatus status = SellerStatus.Pending,
        bool isDeleted = false)
    {
        var seller = new SellerEntity
        {
            Id          = Guid.NewGuid(),
            UserId      = Guid.NewGuid(),
            StoreName   = "Test Store",
            Slug        = $"store-{Guid.NewGuid():N}",
            Description = "Test",
            Status      = status,
            IsDeleted   = isDeleted,
            CreatedAt   = T0,
            UpdatedAt   = T0,
        };
        _db.Sellers.Add(seller);
        await _db.SaveChangesAsync();
        return seller;
    }

    private async Task<SellerKycDocument> SeedDocAsync(
        Guid             sellerId,
        KycDocumentType  type   = KycDocumentType.PanCard,
        KycDocumentStatus status = KycDocumentStatus.Pending,
        DateTime?        createdAt = null)
    {
        var doc = new SellerKycDocument
        {
            Id           = Guid.NewGuid(),
            SellerId     = sellerId,
            DocumentType = type,
            DocumentUrl  = $"https://storage.example.com/{Guid.NewGuid()}.pdf",
            Status       = status,
            CreatedAt    = createdAt ?? T0,
            UpdatedAt    = createdAt ?? T0,
        };
        _db.SellerKycDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }

    // ── TC-SELL-KYC-001: unknown sellerId → KeyNotFoundException ──────────────

    [Fact]
    public async Task Submit_UnknownSellerId_ThrowsKeyNotFound()
    {
        Func<Task> act = () => _sut.SubmitDocumentAsync(
            Guid.NewGuid(),
            new SubmitKycDocumentRequest(KycDocumentType.PanCard, "https://url/pan.pdf"));

        await act.Should().ThrowAsync<KeyNotFoundException>(
            because: "TC-SELL-KYC-001: submitting a document for a non-existent seller must throw");
    }

    // ── TC-SELL-KYC-002: valid seller → document persisted with Status=Pending ─

    [Fact]
    public async Task Submit_ValidSeller_PersistsWithStatusPending()
    {
        var seller = await SeedSellerAsync();

        var dto = await _sut.SubmitDocumentAsync(
            seller.Id,
            new SubmitKycDocumentRequest(KycDocumentType.GstCertificate, "https://url/gst.pdf"));

        dto.Status.Should().Be(KycDocumentStatus.Pending,
            because: "TC-SELL-KYC-002: newly submitted documents must start in Pending state");

        var inDb = await _db.SellerKycDocuments.FindAsync(dto.Id);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be(KycDocumentStatus.Pending);
    }

    // ── TC-SELL-KYC-003: persisted row has correct fields ─────────────────────

    [Fact]
    public async Task Submit_ValidSeller_CorrectFieldsPersisted()
    {
        var seller = await SeedSellerAsync();
        const string url = "https://storage.example.com/pan-card.pdf";

        var dto = await _sut.SubmitDocumentAsync(
            seller.Id,
            new SubmitKycDocumentRequest(KycDocumentType.PanCard, url));

        dto.SellerId.Should().Be(seller.Id,
            because: "TC-SELL-KYC-003: SellerId must match the submitting seller");
        dto.DocumentType.Should().Be(KycDocumentType.PanCard);
        dto.DocumentUrl.Should().Be(url);
        dto.ReviewedBy.Should().BeNull("document has not been reviewed yet");
        dto.ReviewedAt.Should().BeNull("document has not been reviewed yet");
    }

    // ── TC-SELL-KYC-004: cross-tenant isolation ───────────────────────────────

    [Fact]
    public async Task GetDocuments_ReturnsOnlyCallerSellerDocs()
    {
        var sellerA = await SeedSellerAsync();
        var sellerB = await SeedSellerAsync();

        await SeedDocAsync(sellerA.Id, KycDocumentType.PanCard);
        await SeedDocAsync(sellerA.Id, KycDocumentType.GstCertificate);
        await SeedDocAsync(sellerB.Id, KycDocumentType.BankStatement);

        var docs = await _sut.GetDocumentsAsync(sellerA.Id);

        docs.Should().HaveCount(2,
            because: "TC-SELL-KYC-004: GetDocuments must only return documents for the specified seller");
        docs.Should().AllSatisfy(d =>
            d.SellerId.Should().Be(sellerA.Id));
    }

    // ── TC-SELL-KYC-005: multiple docs → ordered newest first ─────────────────

    [Fact]
    public async Task GetDocuments_OrderedNewestFirst()
    {
        var seller = await SeedSellerAsync();

        var older = await SeedDocAsync(seller.Id, createdAt: T0);
        var newer = await SeedDocAsync(seller.Id, createdAt: T0.AddHours(2));

        var docs = await _sut.GetDocumentsAsync(seller.Id);

        docs.Should().HaveCount(2);
        docs[0].Id.Should().Be(newer.Id,
            because: "TC-SELL-KYC-005: most-recently submitted document must appear first");
        docs[1].Id.Should().Be(older.Id);
    }

    // ── TC-SELL-KYC-006: GetPendingReviews returns all pending across sellers ──

    [Fact]
    public async Task GetPendingReviews_ReturnsAllPendingAcrossSellers()
    {
        var sellerA = await SeedSellerAsync();
        var sellerB = await SeedSellerAsync();

        await SeedDocAsync(sellerA.Id, status: KycDocumentStatus.Pending);
        await SeedDocAsync(sellerB.Id, status: KycDocumentStatus.Pending);
        await SeedDocAsync(sellerA.Id, status: KycDocumentStatus.Approved); // should NOT appear

        var pending = await _sut.GetPendingReviewsAsync();

        pending.Should().HaveCount(2,
            because: "TC-SELL-KYC-006: admin must see all pending documents across all sellers");
        pending.Should().AllSatisfy(d =>
            d.Status.Should().Be(KycDocumentStatus.Pending));
    }

    // ── TC-SELL-KYC-007: GetPendingReviews ignores Approved/Rejected docs ─────

    [Fact]
    public async Task GetPendingReviews_ExcludesApprovedAndRejected()
    {
        var seller = await SeedSellerAsync();

        await SeedDocAsync(seller.Id, status: KycDocumentStatus.Approved);
        await SeedDocAsync(seller.Id, status: KycDocumentStatus.Rejected);
        // no Pending docs

        var pending = await _sut.GetPendingReviewsAsync();

        pending.Should().BeEmpty(
            because: "TC-SELL-KYC-007: approved and rejected docs must not appear in the pending queue");
    }

    // ── TC-SELL-KYC-008: ReviewDocument → Approve ────────────────────────────

    [Fact]
    public async Task ReviewDocument_Approve_SetsStatusApprovedAndMeta()
    {
        var seller = await SeedSellerAsync();
        var doc    = await SeedDocAsync(seller.Id);

        var result = await _sut.ReviewDocumentAsync(
            AdminId, doc.Id, new KycReviewRequest(Approved: true, Note: "Looks good"));

        result.Status.Should().Be(KycDocumentStatus.Approved,
            because: "TC-SELL-KYC-008: approving a document must set its status to Approved");
        result.ReviewedBy.Should().Be(AdminId);
        result.ReviewedAt.Should().NotBeNull();
        result.ReviewNote.Should().Be("Looks good");
    }

    // ── TC-SELL-KYC-009: ReviewDocument → Reject ─────────────────────────────

    [Fact]
    public async Task ReviewDocument_Reject_SetsStatusRejectedWithNote()
    {
        var seller = await SeedSellerAsync();
        var doc    = await SeedDocAsync(seller.Id);

        var result = await _sut.ReviewDocumentAsync(
            AdminId, doc.Id,
            new KycReviewRequest(Approved: false, Note: "Image is blurry, please re-upload"));

        result.Status.Should().Be(KycDocumentStatus.Rejected,
            because: "TC-SELL-KYC-009: rejecting a document must set its status to Rejected");
        result.ReviewNote.Should().Be("Image is blurry, please re-upload");
        result.ReviewedBy.Should().Be(AdminId);
    }

    // ── TC-SELL-KYC-010: ReviewDocument → unknown id → KeyNotFoundException ───

    [Fact]
    public async Task ReviewDocument_UnknownId_ThrowsKeyNotFound()
    {
        Func<Task> act = () => _sut.ReviewDocumentAsync(
            AdminId, Guid.NewGuid(), new KycReviewRequest(Approved: true));

        await act.Should().ThrowAsync<KeyNotFoundException>(
            because: "TC-SELL-KYC-010: reviewing a non-existent document must throw");
    }

    // ── TC-SELL-KYC-011: ActivateSeller → no docs → throws ──────────────────

    [Fact]
    public async Task ActivateSeller_NoDocs_ThrowsInvalidOperation()
    {
        var seller = await SeedSellerAsync();

        Func<Task> act = () => _sut.ActivateSellerAsync(AdminId, seller.Id);

        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "TC-SELL-KYC-011: activating a seller with no KYC documents must throw");
    }

    // ── TC-SELL-KYC-012: ActivateSeller → some not approved → throws ─────────

    [Fact]
    public async Task ActivateSeller_SomeDocsNotApproved_ThrowsInvalidOperation()
    {
        var seller = await SeedSellerAsync();

        await SeedDocAsync(seller.Id, status: KycDocumentStatus.Approved);
        await SeedDocAsync(seller.Id, status: KycDocumentStatus.Pending);   // still pending

        Func<Task> act = () => _sut.ActivateSellerAsync(AdminId, seller.Id);

        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "TC-SELL-KYC-012: activation must be blocked until all documents are approved");
    }

    // ── TC-SELL-KYC-013: ActivateSeller → all approved → Status=Active ───────

    [Fact]
    public async Task ActivateSeller_AllDocsApproved_SetsSellerStatusActive()
    {
        var seller = await SeedSellerAsync();

        await SeedDocAsync(seller.Id, KycDocumentType.PanCard,        KycDocumentStatus.Approved);
        await SeedDocAsync(seller.Id, KycDocumentType.GstCertificate, KycDocumentStatus.Approved);

        var profile = await _sut.ActivateSellerAsync(AdminId, seller.Id);

        profile.Status.Should().Be("Active",
            because: "TC-SELL-KYC-013: seller must be activated when all docs are approved");
        profile.ApprovedAt.Should().NotBeNull(
            because: "TC-SELL-KYC-013: ApprovedAt must be recorded on activation");

        // Verify persisted in DB
        var fromDb = await _db.Sellers.FindAsync(seller.Id);
        fromDb!.Status.Should().Be(SellerStatus.Active);
        fromDb.ApprovedAt.Should().NotBeNull();
    }

    // ── TC-SELL-KYC-014: ActivateSeller → unknown sellerId → throws ──────────

    [Fact]
    public async Task ActivateSeller_UnknownSellerId_ThrowsKeyNotFound()
    {
        Func<Task> act = () => _sut.ActivateSellerAsync(AdminId, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>(
            because: "TC-SELL-KYC-014: activating a non-existent seller must throw");
    }
}
