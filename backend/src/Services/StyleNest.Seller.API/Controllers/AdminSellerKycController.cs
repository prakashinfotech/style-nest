/**
 * ENH-SELL-002 — Admin-facing KYC review endpoints.
 *
 * GET api/v1/admin/kyc/pending-reviews           → list all pending KYC submissions
 * PUT api/v1/admin/kyc/documents/{id}/review     → approve or reject a document
 * PUT api/v1/admin/kyc/sellers/{sellerId}/activate → activate seller after all docs approved
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Seller.API.Services;

namespace StyleNest.Seller.API.Controllers;

[ApiController]
[Route("api/v1/admin/kyc")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminSellerKycController(
    ISellerKycService kycService) : ControllerBase
{
    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// ENH-SELL-002 — Returns all KYC documents currently awaiting admin review (Status=Pending),
    /// ordered oldest-first (FIFO review queue).
    /// </summary>
    [HttpGet("pending-reviews")]
    public async Task<IActionResult> GetPendingReviews(CancellationToken ct)
    {
        var docs = await kycService.GetPendingReviewsAsync(ct);
        return Ok(docs);
    }

    /// <summary>
    /// ENH-SELL-002 — Approve or reject a specific KYC document.
    /// Set <c>Approved=true</c> to approve; <c>Approved=false</c> to reject (provide a Note).
    /// </summary>
    [HttpPut("documents/{id:guid}/review")]
    public async Task<IActionResult> ReviewDocument(
        Guid id,
        [FromBody] KycReviewRequest request,
        CancellationToken ct)
    {
        try
        {
            var doc = await kycService.ReviewDocumentAsync(AdminId, id, request, ct);
            return Ok(doc);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// ENH-SELL-002 — Activates a seller account when ALL submitted KYC documents are approved.
    /// Sets Seller.Status=Active and records Seller.ApprovedAt.
    /// Returns 409 Conflict when any documents are still pending or rejected.
    /// </summary>
    [HttpPut("sellers/{sellerId:guid}/activate")]
    public async Task<IActionResult> ActivateSeller(Guid sellerId, CancellationToken ct)
    {
        try
        {
            var profile = await kycService.ActivateSellerAsync(AdminId, sellerId, ct);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { errorCode = "KYC_INCOMPLETE", message = ex.Message });
        }
    }
}
