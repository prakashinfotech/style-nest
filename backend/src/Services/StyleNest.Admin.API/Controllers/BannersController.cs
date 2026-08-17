using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/banners")]
[Authorize(Roles = "Admin")]
public sealed class BannersController(IAdminService adminService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BannerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanners(CancellationToken ct)
    {
        var banners = await adminService.GetBannersAsync(ct);
        return Ok(banners);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BannerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBanner(Guid id, CancellationToken ct)
    {
        var banner = await adminService.GetBannerAsync(id, ct);
        return banner is null ? NotFound() : Ok(banner);
    }

    [HttpPost]
    [ProducesResponseType<BannerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBanner([FromBody] CreateBannerRequest req, CancellationToken ct)
    {
        var banner = await adminService.CreateBannerAsync(req, ct);
        return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, banner);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<BannerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBanner(Guid id, [FromBody] UpdateBannerRequest req, CancellationToken ct)
    {
        var banner = await adminService.UpdateBannerAsync(id, req, ct);
        return banner is null ? NotFound() : Ok(banner);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBanner(Guid id, CancellationToken ct)
    {
        var deleted = await adminService.DeleteBannerAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
