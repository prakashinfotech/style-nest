/**
 * ENH-SELL-002 — Seller-facing KYC endpoints.
 *
 * POST api/v1/seller/kyc/documents        → submit a KYC document (Seller role)
 * GET  api/v1/seller/kyc/documents        → list own submitted documents (Seller role)
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Seller.API.Services;

namespace StyleNest.Seller.API.Controllers;

[ApiController]
[Route("api/v1/seller/kyc")]
[Authorize(Roles = "Seller,Admin,SuperAdmin")]
public sealed class SellerKycController(
    ISellerKycService kycService,
    ISellerService    sellerService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// ENH-SELL-002 — Seller submits a KYC document for admin review.
    /// The DocumentUrl must point to an already-uploaded object (via Media.API).
    /// </summary>
    [HttpPost("documents")]
    public async Task<IActionResult> SubmitDocument(
        [FromBody] SubmitKycDocumentRequest request,
        CancellationToken ct)
    {
        // Resolve the caller's seller profile to get the SellerId
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null)
            return NotFound(new { message = "Seller profile not found for this account." });

        if (string.IsNullOrWhiteSpace(request.DocumentUrl))
            return BadRequest(new { message = "DocumentUrl is required." });

        try
        {
            var doc = await kycService.SubmitDocumentAsync(profile.Id, request, ct);
            return Created($"/api/v1/seller/kyc/documents/{doc.Id}", doc);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// ENH-SELL-002 — Returns all KYC documents submitted by the authenticated seller.
    /// </summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetMyDocuments(CancellationToken ct)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null)
            return NotFound(new { message = "Seller profile not found for this account." });

        var docs = await kycService.GetDocumentsAsync(profile.Id, ct);
        return Ok(docs);
    }
}
