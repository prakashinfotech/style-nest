using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.User.API.DTOs;
using StyleNest.User.API.Services;

namespace StyleNest.User.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IErasureService _erasureService;

    public UsersController(IUserService userService, IErasureService erasureService)
    {
        _userService    = userService;
        _erasureService = erasureService;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _userService.GetProfileAsync(CurrentUserId, ct);
        return result.IsFailure ? NotFound(result.Error) : Ok(result.Value);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto, CancellationToken ct)
    {
        var result = await _userService.UpdateProfileAsync(CurrentUserId, dto, ct);
        return result.IsFailure ? NotFound(result.Error) : Ok(result.Value);
    }

    [HttpGet("me/addresses")]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
    {
        var result = await _userService.GetAddressesAsync(CurrentUserId, ct);
        return Ok(result.Value);
    }

    [HttpPost("me/addresses")]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequestDto dto, CancellationToken ct)
    {
        var result = await _userService.CreateAddressAsync(CurrentUserId, dto, ct);
        return result.IsFailure
            ? BadRequest(result.Error)
            : CreatedAtAction(nameof(GetAddresses), result.Value);
    }

    [HttpPut("me/addresses/{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressRequestDto dto, CancellationToken ct)
    {
        var result = await _userService.UpdateAddressAsync(CurrentUserId, id, dto, ct);
        return result.IsFailure ? NotFound(result.Error) : Ok(result.Value);
    }

    [HttpPost("me/addresses/{id:guid}/set-default")]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken ct)
    {
        var result = await _userService.SetDefaultAddressAsync(CurrentUserId, id, ct);
        return result.IsFailure ? NotFound(result.Error) : NoContent();
    }

    [HttpDelete("me/addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var result = await _userService.DeleteAddressAsync(CurrentUserId, id, ct);
        return result.IsFailure ? NotFound(result.Error) : NoContent();
    }

    [HttpGet("me/wishlist")]
    public async Task<IActionResult> GetWishlist(CancellationToken ct)
    {
        var result = await _userService.GetWishlistAsync(CurrentUserId, ct);
        return Ok(result.Value);
    }

    [HttpPost("me/wishlist/{productId:guid}")]
    public async Task<IActionResult> AddToWishlist(Guid productId, CancellationToken ct)
    {
        var result = await _userService.AddToWishlistAsync(CurrentUserId, productId, ct);
        return result.IsFailure ? NotFound(result.Error) : NoContent();
    }

    [HttpDelete("me/wishlist/{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken ct)
    {
        await _userService.RemoveFromWishlistAsync(CurrentUserId, productId, ct);
        return NoContent();
    }

    /// <summary>ENH-AUTH-011 — PDPB / GDPR Right-to-Erasure.</summary>
    [HttpDelete("me")]
    public async Task<IActionResult> EraseAccount(CancellationToken ct)
    {
        var result = await _erasureService.EraseUserAsync(CurrentUserId, ct);
        return result.IsFailure ? NotFound(result.Error) : NoContent();
    }
}
