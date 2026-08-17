using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.DTOs;
using StyleNest.SharedKernel.DTOs;

namespace StyleNest.Seller.API.Services;

public class SellerService(AppDbContext db) : ISellerService
{
    public async Task<SellerProfileDto?> GetProfileAsync(Guid userId)
    {
        var seller = await db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        return seller is null ? null : MapProfile(seller);
    }

    public async Task<SellerProfileDto> UpdateProfileAsync(Guid userId, UpdateSellerProfileRequest request)
    {
        var seller = await db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException("Seller profile not found.");

        seller.StoreName         = request.StoreName;
        seller.Description       = request.Description;
        seller.LogoUrl           = request.LogoUrl;
        seller.GstNumber         = request.GstNumber;
        seller.PanNumber         = request.PanNumber;
        seller.BankAccountNumber = request.BankAccountNumber;
        seller.BankIfsc          = request.BankIfsc;

        await db.SaveChangesAsync();
        return MapProfile(seller);
    }

    public async Task<SellerDashboardDto> GetDashboardAsync(Guid sellerId)
    {
        var productCount = await db.Products.CountAsync(p => p.SellerId == sellerId);

        var sellerVariantIds = await db.SellerInventories
            .Where(si => si.SellerId == sellerId)
            .Select(si => si.ProductVariantId)
            .ToListAsync();

        var allOrders = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .Where(o => o.Items.Any(i => sellerVariantIds.Contains(i.ProductVariantId)))
            .ToListAsync();

        var pending  = allOrders.Count(o => o.Status == OrderStatus.Pending);
        var revenue  = allOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
        var monthRev = allOrders
            .Where(o => o.Status != OrderStatus.Cancelled && o.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
            .Sum(o => o.TotalAmount);

        var lowStock = await db.SellerInventories
            .CountAsync(si => si.SellerId == sellerId && si.Stock <= si.LowStockThreshold);

        var recent = allOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderDto(
                o.Id, o.OrderNumber, o.TotalAmount, o.Status.ToString(), o.CreatedAt));

        return new SellerDashboardDto(
            productCount, allOrders.Count, pending,
            revenue, monthRev, lowStock, recent);
    }

    public async Task<SellerAnalyticsDto> GetAnalyticsAsync(Guid sellerId, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var sellerVariantIds = await db.SellerInventories
            .Where(si => si.SellerId == sellerId)
            .Select(si => si.ProductVariantId)
            .ToListAsync();

        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Cancelled &&
                   o.CreatedAt >= since &&
                   o.Items.Any(i => sellerVariantIds.Contains(i.ProductVariantId)))
            .ToListAsync();

        var total = orders.Sum(o => o.TotalAmount);
        var month = orders.Where(o => o.CreatedAt >= DateTime.UtcNow.AddMonths(-1)).Sum(o => o.TotalAmount);
        var week  = orders.Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-7)).Sum(o => o.TotalAmount);

        var byDay = orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailyRevenueDto(g.Key, g.Sum(o => o.TotalAmount), g.Count()))
            .OrderBy(d => d.Date);

        return new SellerAnalyticsDto(
            total, month, week, orders.Count,
            orders.Count(o => o.CreatedAt >= DateTime.UtcNow.AddMonths(-1)),
            byDay, Enumerable.Empty<TopProductDto>());
    }

    public async Task<PagedResult<SellerProductDto>> GetProductsAsync(Guid sellerId, int page, int pageSize)
    {
        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<SellerProductDto>(
            items.Select(MapProduct).ToList(), total, page, pageSize);
    }

    public async Task<SellerProductDto> CreateProductAsync(Guid sellerId, CreateSellerProductRequest request)
    {
        var slug    = GenerateSlug(request.Name);
        var product = new Product
        {
            Id              = Guid.NewGuid(),
            Name            = request.Name,
            Slug            = slug,
            Description     = request.Description,
            BasePrice       = request.BasePrice,
            DiscountedPrice = request.DiscountedPrice,
            CategoryId      = request.CategoryId,
            BrandId         = request.BrandId,
            SellerId        = sellerId,
            IsActive        = false,
        };

        foreach (var v in request.Variants)
        {
            product.Variants.Add(new ProductVariant
            {
                Id            = Guid.NewGuid(),
                ProductId     = product.Id,
                Sku           = v.Sku,
                Size          = v.Size,
                Colour        = v.Colour,
                StockQuantity = v.StockQuantity,
                PriceOverride = v.PriceOverride,
            });
        }

        int imgOrder = 0;
        foreach (var url in request.ImageUrls)
        {
            product.Images.Add(new ProductImage
            {
                Id           = Guid.NewGuid(),
                ProductId    = product.Id,
                Url          = url,
                DisplayOrder = imgOrder++,
            });
        }

        if (request.Attributes is not null)
        {
            foreach (var a in request.Attributes)
            {
                product.Attributes.Add(new ProductAttribute
                {
                    Id                    = Guid.NewGuid(),
                    ProductId             = product.Id,
                    AttributeDefinitionId = a.AttributeDefinitionId,
                    Value                 = a.Value,
                });
            }
        }

        db.Products.Add(product);

        foreach (var v in product.Variants)
        {
            db.SellerInventories.Add(new SellerInventory
            {
                Id               = Guid.NewGuid(),
                SellerId         = sellerId,
                ProductVariantId = v.Id,
                Stock            = v.StockQuantity,
                Price            = v.PriceOverride ?? request.BasePrice,
            });
        }

        await db.SaveChangesAsync();

        var created = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .FirstAsync(p => p.Id == product.Id);

        return MapProduct(created);
    }

    public async Task<SellerProductDto> UpdateProductAsync(Guid sellerId, Guid productId, UpdateSellerProductRequest request)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId)
            ?? throw new KeyNotFoundException("Product not found or not owned by seller.");

        product.Name            = request.Name;
        product.Description     = request.Description;
        product.BasePrice       = request.BasePrice;
        product.DiscountedPrice = request.DiscountedPrice;
        product.IsActive        = request.IsActive;

        if (request.Attributes is not null)
        {
            db.ProductAttributes.RemoveRange(product.Attributes);
            foreach (var a in request.Attributes)
            {
                product.Attributes.Add(new ProductAttribute
                {
                    Id                    = Guid.NewGuid(),
                    ProductId             = product.Id,
                    AttributeDefinitionId = a.AttributeDefinitionId,
                    Value                 = a.Value,
                });
            }
        }

        await db.SaveChangesAsync();
        return MapProduct(product);
    }

    public async Task DeleteProductAsync(Guid sellerId, Guid productId)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId)
            ?? throw new KeyNotFoundException("Product not found or not owned by seller.");

        product.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<InventoryItemDto>> GetInventoryAsync(Guid sellerId)
    {
        var items      = await db.SellerInventories.Where(si => si.SellerId == sellerId).ToListAsync();
        var variantIds = items.Select(i => i.ProductVariantId).ToList();
        var variants   = await db.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToListAsync();

        return items.Select(i =>
        {
            var v = variants.FirstOrDefault(x => x.Id == i.ProductVariantId);
            return new InventoryItemDto(
                i.Id, i.ProductVariantId,
                v?.Sku ?? "", v?.Size ?? "", v?.Colour ?? "",
                i.Stock, i.Price, i.LowStockThreshold,
                i.Stock <= i.LowStockThreshold);
        });
    }

    public async Task<InventoryItemDto> UpdateInventoryAsync(Guid sellerId, Guid inventoryId, UpdateInventoryRequest request)
    {
        var item = await db.SellerInventories
            .FirstOrDefaultAsync(si => si.Id == inventoryId && si.SellerId == sellerId)
            ?? throw new KeyNotFoundException("Inventory item not found.");

        item.Stock = request.Stock;
        if (request.Price.HasValue)             item.Price             = request.Price.Value;
        if (request.LowStockThreshold.HasValue) item.LowStockThreshold = request.LowStockThreshold.Value;

        var variant = await db.ProductVariants.FindAsync(item.ProductVariantId);
        if (variant is not null) variant.StockQuantity = request.Stock;

        await db.SaveChangesAsync();

        var v = await db.ProductVariants.FindAsync(item.ProductVariantId);
        return new InventoryItemDto(
            item.Id, item.ProductVariantId,
            v?.Sku ?? "", v?.Size ?? "", v?.Colour ?? "",
            item.Stock, item.Price, item.LowStockThreshold,
            item.Stock <= item.LowStockThreshold);
    }

    public async Task<PagedResult<SellerOrderDto>> GetOrdersAsync(Guid sellerId, int page, int pageSize, string? status)
    {
        var sellerVariantIds = await db.SellerInventories
            .Where(si => si.SellerId == sellerId)
            .Select(si => si.ProductVariantId)
            .ToListAsync();

        var query = db.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .Where(o => o.Items.Any(i => sellerVariantIds.Contains(i.ProductVariantId)));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var statusEnum))
            query = query.Where(o => o.Status == statusEnum);

        var total  = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SellerOrderDto>(
            orders.Select(o => MapOrder(o, sellerVariantIds)).ToList(), total, page, pageSize);
    }

    public async Task<SellerOrderDto?> GetOrderAsync(Guid sellerId, Guid orderId)
    {
        var sellerVariantIds = await db.SellerInventories
            .Where(si => si.SellerId == sellerId)
            .Select(si => si.ProductVariantId)
            .ToListAsync();

        var order = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId &&
                o.Items.Any(i => sellerVariantIds.Contains(i.ProductVariantId)));

        return order is null ? null : MapOrder(order, sellerVariantIds);
    }

    public async Task UpdateOrderStatusAsync(Guid sellerId, Guid orderId, UpdateOrderStatusRequest request)
    {
        var sellerVariantIds = await db.SellerInventories
            .Where(si => si.SellerId == sellerId)
            .Select(si => si.ProductVariantId)
            .ToListAsync();

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId &&
                o.Items.Any(i => sellerVariantIds.Contains(i.ProductVariantId)))
            ?? throw new KeyNotFoundException("Order not found.");

        if (Enum.TryParse<OrderStatus>(request.Status, out var statusEnum))
            order.Status = statusEnum;

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id      = Guid.NewGuid(),
            OrderId = order.Id,
            Status  = order.Status,
            Note    = request.Notes,
        });

        await db.SaveChangesAsync();
    }

    public async Task<PagedResult<PayoutDto>> GetPayoutsAsync(Guid sellerId, int page, int pageSize)
    {
        var query = db.SellerPayouts
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<PayoutDto>(
            items.Select(p => new PayoutDto(
                p.Id, p.Amount, p.Status.ToString(),
                p.TransactionReference, p.ProcessedAt,
                p.Notes, p.CreatedAt)).ToList(),
            total, page, pageSize);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SellerProfileDto MapProfile(Infrastructure.Entities.Seller.Seller s) => new(
        s.Id, s.StoreName, s.Slug, s.Description, s.LogoUrl,
        s.GstNumber, s.PanNumber, s.Status.ToString(),
        s.CommissionRate, s.ApprovedAt, s.CreatedAt);

    private static SellerProductDto MapProduct(Product p) => new(
        p.Id, p.Name, p.Slug, p.BasePrice, p.DiscountedPrice,
        p.Category?.Name ?? "", p.Brand?.Name ?? "",
        p.Variants.Sum(v => v.StockQuantity),
        p.IsActive, p.AverageRating, p.ReviewCount, p.CreatedAt);

    private static SellerOrderDto MapOrder(Order o, List<Guid> sellerVariantIds) => new(
        o.Id, o.OrderNumber,
        o.User is not null ? $"{o.User.FirstName} {o.User.LastName}" : "Customer",
        o.Status.ToString(), o.TotalAmount, o.CreatedAt,
        o.Items
            .Where(i => sellerVariantIds.Contains(i.ProductVariantId))
            .Select(i => new SellerOrderItemDto(
                i.ProductVariantId, i.ProductName,
                i.VariantDetails ?? "", i.Quantity, i.UnitPrice)));

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("&", "and");
        return $"{slug}-{Guid.NewGuid().ToString()[..8]}";
    }
}
