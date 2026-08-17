using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

/// <summary>ENH-CAT-002 — Flash Sale Module endpoints.</summary>
[ApiController]
[Route("api/v1/flash-sales")]
public sealed class FlashSaleController(IFlashSaleService flashSaleService) : ControllerBase
{
    /// <summary>
    /// ENH-CAT-002 — Returns all currently active flash sales with server-computed countdown.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var sales = await flashSaleService.GetActiveSalesAsync(ct);
        return Ok(sales);
    }

    /// <summary>
    /// ENH-CAT-002 — Returns all items for the given flash sale,
    /// sorted by scarcity (fewest remaining first; sold-out items last).
    /// </summary>
    [HttpGet("{id:guid}/items")]
    public async Task<IActionResult> GetItems(Guid id, CancellationToken ct)
    {
        var items = await flashSaleService.GetFlashSaleItemsAsync(id, ct);
        if (items.Count == 0)
            return NotFound(new { message = $"Flash sale {id} not found or has no items." });

        return Ok(items);
    }
}
