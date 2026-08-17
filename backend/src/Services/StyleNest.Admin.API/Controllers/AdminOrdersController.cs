using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Roles = "Admin")]
public sealed class AdminOrdersController(
    IAdminService adminService,
    IValidator<UpdateOrderStatusRequest> statusValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminOrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var orders = await adminService.GetAdminOrdersAsync(ct);
        return Ok(orders);
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<AdminOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest req, CancellationToken ct)
    {
        var validation = await statusValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var order = await adminService.UpdateOrderStatusAsync(id, req.Status, ct);
        return order is null ? NotFound() : Ok(order);
    }
}
