using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

public interface ISellerCatalogService
{
    Task<IReadOnlyList<SellerProductDto>> GetMyProductsAsync(Guid sellerId, CancellationToken ct = default);
    Task<SellerProductDto>                CreateProductAsync(Guid sellerId, CreateSellerProductRequest req, CancellationToken ct = default);
    Task<SellerProductDto?>               UpdateProductAsync(Guid sellerId, Guid productId, UpdateSellerProductRequest req, CancellationToken ct = default);
    Task<bool>                            DeleteProductAsync(Guid sellerId, Guid productId, CancellationToken ct = default);
}

public sealed class SellerCatalogService(AppDbContext db) : ISellerCatalogService
{
    public async Task<IReadOnlyList<SellerProductDto>> GetMyProductsAsync(Guid sellerId, CancellationToken ct = default)
    {
        var products = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .AsNoTracking()
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return products.Select(MapToDto).ToList();
    }

    public async Task<SellerProductDto> CreateProductAsync(Guid sellerId, CreateSellerProductRequest req, CancellationToken ct = default)
    {
        var slug = await GenerateUniqueSlugAsync(req.Name, ct);

        // MRP > Price → store MRP as BasePrice and Price as DiscountedPrice
        decimal basePrice;
        decimal? discountedPrice;
        if (req.Mrp.HasValue && req.Mrp.Value > req.Price)
        {
            basePrice       = req.Mrp.Value;
            discountedPrice = req.Price;
        }
        else
        {
            basePrice       = req.Price;
            discountedPrice = null;
        }

        var product = new Product
        {
            Id              = Guid.NewGuid(),
            SellerId        = sellerId,
            Name            = req.Name,
            Slug            = slug,
            Description     = req.Description,
            BasePrice       = basePrice,
            DiscountedPrice = discountedPrice,
            CategoryId      = req.CategoryId,
            BrandId         = req.BrandId,
            IsActive        = true,
            AverageRating   = 0,
            ReviewCount     = 0,
        };

        // Default variant so the product appears in stock
        product.Variants.Add(new ProductVariant
        {
            Id            = Guid.NewGuid(),
            ProductId     = product.Id,
            Size          = "Free Size",
            Sku           = $"SKU-{product.Id:N}",
            StockQuantity = 10,
        });

        for (var i = 0; i < req.ImageUrls.Count; i++)
        {
            product.Images.Add(new ProductImage
            {
                Id           = Guid.NewGuid(),
                ProductId    = product.Id,
                Url          = req.ImageUrls[i],
                DisplayOrder = i,
                IsPrimary    = i == 0,
            });
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        // Reload with navigation properties for the response DTO
        var saved = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .AsNoTracking()
            .FirstAsync(p => p.Id == product.Id, ct);

        return MapToDto(saved);
    }

    public async Task<SellerProductDto?> UpdateProductAsync(Guid sellerId, Guid productId, UpdateSellerProductRequest req, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId, ct);

        if (product is null) return null;

        product.Name        = req.Name;
        product.Description = req.Description;
        product.IsActive    = req.IsActive;

        if (req.Mrp.HasValue && req.Mrp.Value > req.Price)
        {
            product.BasePrice       = req.Mrp.Value;
            product.DiscountedPrice = req.Price;
        }
        else
        {
            product.BasePrice       = req.Price;
            product.DiscountedPrice = null;
        }

        await db.SaveChangesAsync(ct);
        return MapToDto(product);
    }

    public async Task<bool> DeleteProductAsync(Guid sellerId, Guid productId, CancellationToken ct = default)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId, ct);

        if (product is null) return false;

        product.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static SellerProductDto MapToDto(Product p) =>
        new(
            p.Id,
            p.Name,
            p.Slug,
            p.Description ?? string.Empty,
            p.DiscountedPrice ?? p.BasePrice,
            p.DiscountedPrice.HasValue ? (decimal?)p.BasePrice : null,
            p.BrandId,
            p.Brand?.Name ?? string.Empty,
            p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).ToList(),
            p.Variants.Any(v => v.StockQuantity > 0),
            p.IsActive,
            p.CreatedAt
        );

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
        var slug     = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";

        // Extremely unlikely collision but guard it anyway
        while (await db.Products.IgnoreQueryFilters().AnyAsync(p => p.Slug == slug, ct))
            slug = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";

        return slug;
    }
}
