using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Order.API.DTOs;
using StyleNest.Order.API.Exceptions;
using StyleNest.Order.API.Services;

namespace StyleNest.Order.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public sealed class OrdersController(
    IOrderService orderService,
    IValidator<PlaceOrderRequest> validator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("buy-now")]
    public async Task<IActionResult> BuyNow([FromBody] BuyNowRequest request, CancellationToken ct)
    {
        if (request.ProductId == Guid.Empty || request.Quantity < 1)
            return BadRequest(new { message = "Invalid product or quantity." });

        try
        {
            var order = await orderService.BuyNowAsync(UserId, request, ct);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (CheckoutEmailUnverifiedException ex) { return BuildEmailUnverifiedResponse(ex); }
        catch (ConcurrentCheckoutException ex)      { return Conflict(new { errorCode = "CHECKOUT_CONFLICT", message = ex.Message }); }
        catch (InventoryValidationException ex)     { return UnprocessableEntity(BuildOosResponse(ex)); }
        catch (KeyNotFoundException ex)             { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex)        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        try
        {
            var order = await orderService.PlaceOrderAsync(UserId, request, ct);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (CheckoutEmailUnverifiedException ex) { return BuildEmailUnverifiedResponse(ex); }
        catch (ConcurrentCheckoutException ex)      { return Conflict(new { errorCode = "CHECKOUT_CONFLICT", message = ex.Message }); }
        catch (InventoryValidationException ex)     { return UnprocessableEntity(BuildOosResponse(ex)); }
        catch (InvalidOperationException ex)        { return BadRequest(new { message = ex.Message }); }
    }

    private ObjectResult BuildEmailUnverifiedResponse(CheckoutEmailUnverifiedException ex) =>
        StatusCode(StatusCodes.Status403Forbidden, new
        {
            errorCode      = "CHECKOUT_EMAIL_UNVERIFIED",
            message        = ex.Message,
            verifyEmailUrl = "/api/v1/auth/email/verify/send",
        });

    private static object BuildOosResponse(InventoryValidationException ex) => new
    {
        errorCode = "INVENTORY_VALIDATION_FAILED",
        message   = ex.Message,
        outOfStockItems = ex.OutOfStockItems.Select(i => new
        {
            variantId         = i.VariantId,
            productName       = i.ProductName,
            variantDetails    = i.VariantDetails,
            requestedQuantity = i.RequestedQuantity,
            availableQuantity = i.AvailableQuantity,
        }),
    };

    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var orders = await orderService.GetOrdersAsync(UserId, ct);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var order = await orderService.GetOrderAsync(UserId, id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken ct)
    {
        try
        {
            await orderService.CancelOrderAsync(UserId, id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)       { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
