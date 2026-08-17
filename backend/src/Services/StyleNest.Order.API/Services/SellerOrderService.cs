using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.DTOs;

namespace StyleNest.Order.API.Services;

public interface ISellerOrderService
{
    Task<IReadOnlyList<SellerOrderDto>> GetSellerOrdersAsync(Guid sellerId, CancellationToken ct = default);
}

public sealed class SellerOrderService(AppDbContext db) : ISellerOrderService
{
    public async Task<IReadOnlyList<SellerOrderDto>> GetSellerOrdersAsync(Guid sellerId, CancellationToken ct = default)
    {
        // Collect the product IDs owned by this seller
        var sellerProductIds = await db.Products
            .AsNoTracking()
            .Where(p => p.SellerId == sellerId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (sellerProductIds.Count == 0)
            return [];

        // Find orders that contain at least one item belonging to this seller
        var orders = await db.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
            .AsNoTracking()
            .Where(o => o.Items.Any(i => sellerProductIds.Contains(i.ProductVariant.ProductId)))
            .OrderByDescending(o => o.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        return orders.Select(o =>
        {
            var sellerItems = o.Items
                .Where(i => sellerProductIds.Contains(i.ProductVariant.ProductId))
                .Select(i => new SellerOrderItemDto(
                    i.ProductVariant.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice))
                .ToList();

            var sellerTotal = sellerItems.Sum(i => i.UnitPrice * i.Quantity);

            return new SellerOrderDto(
                o.Id,
                o.OrderNumber,
                $"{o.User.FirstName} {o.User.LastName}",
                o.Status.ToString(),
                sellerTotal,
                o.CreatedAt,
                sellerItems);
        }).ToList();
    }
}
