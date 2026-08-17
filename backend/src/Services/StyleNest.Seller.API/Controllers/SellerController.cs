using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StyleNest.Seller.API.DTOs;
using StyleNest.Seller.API.Services;

namespace StyleNest.Seller.API.Controllers;

[ApiController]
[Route("api/v1/seller")]
[Authorize(Roles = "Seller,Admin,SuperAdmin")]
public class SellerController(ISellerService sellerService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateSellerProfileRequest request)
    {
        var profile = await sellerService.UpdateProfileAsync(UserId, request);
        return Ok(profile);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var dashboard = await sellerService.GetDashboardAsync(profile.Id);
        return Ok(dashboard);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] int days = 30)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var analytics = await sellerService.GetAnalyticsAsync(profile.Id, days);
        return Ok(analytics);
    }

    // ── Products ──────────────────────────────────────────────────────────

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var products = await sellerService.GetProductsAsync(profile.Id, page, pageSize);
        return Ok(products);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateSellerProductRequest request)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var product = await sellerService.CreateProductAsync(profile.Id, request);
        return CreatedAtAction(nameof(GetProducts), new { }, product);
    }

    [HttpPut("products/{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateSellerProductRequest request)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var product = await sellerService.UpdateProductAsync(profile.Id, productId, request);
        return Ok(product);
    }

    [HttpDelete("products/{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        await sellerService.DeleteProductAsync(profile.Id, productId);
        return NoContent();
    }

    // ── Inventory ────────────────────────────────────────────────────────

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var inventory = await sellerService.GetInventoryAsync(profile.Id);
        return Ok(inventory);
    }

    [HttpPut("inventory/{inventoryId:guid}")]
    public async Task<IActionResult> UpdateInventory(Guid inventoryId, [FromBody] UpdateInventoryRequest request)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var item = await sellerService.UpdateInventoryAsync(profile.Id, inventoryId, request);
        return Ok(item);
    }

    // ── Orders ───────────────────────────────────────────────────────────

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var orders = await sellerService.GetOrdersAsync(profile.Id, page, pageSize, status);
        return Ok(orders);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var order = await sellerService.GetOrderAsync(profile.Id, orderId);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("orders/{orderId:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        await sellerService.UpdateOrderStatusAsync(profile.Id, orderId, request);
        return NoContent();
    }

    // ── Payouts ──────────────────────────────────────────────────────────

    [HttpGet("payouts")]
    public async Task<IActionResult> GetPayouts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profile = await sellerService.GetProfileAsync(UserId);
        if (profile is null) return NotFound("Seller profile not found.");
        var payouts = await sellerService.GetPayoutsAsync(profile.Id, page, pageSize);
        return Ok(payouts);
    }
}
