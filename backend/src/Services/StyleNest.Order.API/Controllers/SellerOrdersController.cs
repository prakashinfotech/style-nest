using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Order.API.DTOs;
using StyleNest.Order.API.Services;

namespace StyleNest.Order.API.Controllers;

[ApiController]
[Route("api/v1/seller/orders")]
[Authorize(Roles = "Seller")]
public sealed class SellerOrdersController(ISellerOrderService sellerOrderService) : ControllerBase
{
    private Guid SellerId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SellerOrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders(CancellationToken ct)
    {
        var orders = await sellerOrderService.GetSellerOrdersAsync(SellerId, ct);
        return Ok(orders);
    }
}
