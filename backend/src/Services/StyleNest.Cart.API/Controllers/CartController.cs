using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Cart.API.DTOs;
using StyleNest.Cart.API.Exceptions;
using StyleNest.Cart.API.Services;

namespace StyleNest.Cart.API.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public sealed class CartController(
    ICartService cartService,
    IValidator<AddToCartRequest> addValidator,
    IValidator<UpdateCartItemRequest> updateValidator,
    IValidator<ApplyCouponRequest> couponValidator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var cart = await cartService.GetCartAsync(UserId, ct);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request, CancellationToken ct)
    {
        var validation = await addValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var cart = await cartService.AddItemAsync(UserId, request.ProductId, request.Size, request.Colour, request.Quantity, ct);
        return Ok(cart);
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        try
        {
            var cart = await cartService.UpdateItemAsync(UserId, id, request.Quantity, ct);
            return Ok(cart);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, CancellationToken ct)
    {
        try
        {
            var cart = await cartService.RemoveItemAsync(UserId, id, ct);
            return Ok(cart);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request, CancellationToken ct)
    {
        var validation = await couponValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        try
        {
            var cart = await cartService.ApplyCouponAsync(UserId, request.Code, ct);
            return Ok(cart);
        }
        catch (CouponValidationException ex)
        {
            return BadRequest(new { errorCode = ex.ErrorCode, message = ex.Message });
        }
    }
}
