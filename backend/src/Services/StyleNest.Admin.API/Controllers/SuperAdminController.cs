using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public sealed class SuperAdminController(IAdminService adminService) : ControllerBase
{
    // ── Admin User Management ─────────────────────────────────────────────────

    [HttpGet("admins")]
    [ProducesResponseType<IReadOnlyList<AdminSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdmins(CancellationToken ct)
    {
        var admins = await adminService.GetAdminStaffAsync(ct);
        return Ok(admins);
    }

    [HttpPost("admins")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminUserRequest req, CancellationToken ct)
    {
        var (success, error) = await adminService.CreateAdminUserAsync(req, ct);
        return success
            ? StatusCode(StatusCodes.Status201Created)
            : BadRequest(new { error });
    }

    [HttpPost("users/{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken ct)
    {
        var ok = await adminService.SuspendUserAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Seller Management ────────────────────────────────────────────────────

    [HttpGet("sellers")]
    [ProducesResponseType<IReadOnlyList<SellerSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSellers([FromQuery] string? status, CancellationToken ct)
    {
        var sellers = await adminService.GetSellersAsync(status, ct);
        return Ok(sellers);
    }

    [HttpPost("sellers/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveSeller(Guid id, [FromBody] ApproveSellersRequest req, CancellationToken ct)
    {
        var ok = await adminService.UpdateSellerStatusAsync(id, req.Approve, req.RejectionReason, ct);
        return ok ? NoContent() : NotFound();
    }
}
