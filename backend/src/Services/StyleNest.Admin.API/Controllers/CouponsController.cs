using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/coupons")]
[Authorize(Roles = "Admin")]
public sealed class CouponsController(IAdminService adminService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CouponDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCoupons(CancellationToken ct)
    {
        var coupons = await adminService.GetCouponsAsync(ct);
        return Ok(coupons);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CouponDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCoupon(Guid id, CancellationToken ct)
    {
        var coupon = await adminService.GetCouponAsync(id, ct);
        return coupon is null ? NotFound() : Ok(coupon);
    }

    [HttpPost]
    [ProducesResponseType<CouponDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest req, CancellationToken ct)
    {
        var coupon = await adminService.CreateCouponAsync(req, ct);
        return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, coupon);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CouponDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] UpdateCouponRequest req, CancellationToken ct)
    {
        var coupon = await adminService.UpdateCouponAsync(id, req, ct);
        return coupon is null ? NotFound() : Ok(coupon);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCoupon(Guid id, CancellationToken ct)
    {
        var deleted = await adminService.DeleteCouponAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
