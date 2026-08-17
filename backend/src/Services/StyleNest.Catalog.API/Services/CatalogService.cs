using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.DTOs;

namespace StyleNest.Catalog.API.Services;

public interface ICatalogService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryDto query, CancellationToken ct = default);
    Task<ProductDto?>    GetProductAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BrandDto>>    GetBrandsAsync(CancellationToken ct = default);

    Task<PagedResult<ReviewDto>> GetReviewsAsync(Guid productId, int page, int pageSize, CancellationToken ct = default);
    Task<ReviewDto>              CreateReviewAsync(Guid productId, Guid userId, string author, CreateReviewRequest req, CancellationToken ct = default);

    Task<IReadOnlyList<ProductDto>> GetRelatedProductsAsync(Guid productId, int limit, CancellationToken ct = default);

    Task<ProductDto>   CreateProductAsync(CreateProductRequest req, CancellationToken ct = default);
    Task<ProductDto?>  UpdateProductAsync(Guid id, UpdateProductRequest req, CancellationToken ct = default);
    Task<CategoryDto>  CreateCategoryAsync(CreateCategoryRequest req, CancellationToken ct = default);
    Task<BrandDto>     CreateBrandAsync(CreateBrandRequest req, CancellationToken ct = default);

    Task<IReadOnlyList<AttributeDefinitionDto>> GetCategoryAttributesAsync(Guid categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<AttributeDefinitionDto>> GetAllAttributesAsync(CancellationToken ct = default);
    Task<AttributeDefinitionDto> CreateAttributeDefinitionAsync(CreateAttributeDefinitionRequest req, CancellationToken ct = default);
    Task MapCategoryAttributeAsync(Guid categoryId, MapCategoryAttributeRequest req, CancellationToken ct = default);
}

public sealed class CatalogService(
    AppDbContext             db,
    IMapper                  mapper,
    ICacheService            cache,
    ISearchAnalyticsService  searchAnalytics) : ICatalogService
{
    private static readonly TimeSpan ProductListTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CategoryTtl    = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan BrandTtl       = TimeSpan.FromMinutes(60);

    private static string ProductListKey(ProductQueryDto q) =>
        $"catalog:products:{q.CategoryId}:{q.BrandId}:{q.Search}:{q.Sort}:{q.Page}:{q.PageSize}:{q.MinPrice}:{q.MaxPrice}:{q.MinDiscount}";

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryDto query, CancellationToken ct = default)
    {
        var cacheKey = ProductListKey(query);
        var cached = await cache.GetAsync<PagedResult<ProductDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var q = db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Attributes).ThenInclude(a => a.AttributeDefinition)
            .AsNoTracking()
            .Where(p => p.IsActive); // storefront should never show inactive products

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        if (query.BrandId.HasValue)
            q = q.Where(p => p.BrandId == query.BrandId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(search) || 
                             (p.Description != null && p.Description.Contains(search)) ||
                             p.Brand.Name.Contains(search));
        }

        if (query.MinPrice.HasValue)
            q = q.Where(p => p.BasePrice >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(p => p.BasePrice <= query.MaxPrice.Value);

        if (query.MinDiscount.HasValue)
        {
            var pct = (decimal)query.MinDiscount.Value;
            // Keep only products where DiscountedPrice exists and the discount % >= requested minimum.
            // Expressed without division: (BasePrice - DiscountedPrice) * 100 >= BasePrice * pct
            q = q.Where(p => p.DiscountedPrice != null &&
                              (p.BasePrice - p.DiscountedPrice.Value) * 100m >= p.BasePrice * pct);
        }

        // ENH-ADMIN-005 — EAV dynamic attribute filtering
        // For each requested attribute: AND-intersect products that match any of the specified values.
        if (query.AttributeFilters is { Count: > 0 })
        {
            foreach (var (attrName, values) in query.AttributeFilters)
            {
                if (values is null || values.Count == 0) continue;

                // Sub-query: product IDs that have this attribute with one of the allowed values
                var matchingIds = db.ProductAttributes
                    .AsNoTracking()
                    .Where(pa => pa.AttributeDefinition.Name == attrName
                                 && values.Contains(pa.Value))
                    .Select(pa => pa.ProductId);

                q = q.Where(p => matchingIds.Contains(p.Id));
            }
        }

        q = query.Sort switch
        {
            "price_asc"  => q.OrderBy(p => p.BasePrice),
            "price_desc" => q.OrderByDescending(p => p.BasePrice),
            "newest"     => q.OrderByDescending(p => p.CreatedAt),
            "rating"     => q.OrderByDescending(p => p.AverageRating),
            _            => q.OrderByDescending(p => p.CreatedAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(p => mapper.Map<ProductDto>(p)).ToList();
        var result = new PagedResult<ProductDto>(dtos, total, query.Page, query.PageSize);
        await cache.SetAsync(cacheKey, result, ProductListTtl, ct);

        // ENH-SRCH-004 — Record search analytics (fire-and-forget; failure is swallowed inside the service)
        if (!string.IsNullOrWhiteSpace(query.Search))
            await searchAnalytics.RecordSearchAsync(query.Search.Trim(), total > 0, ct);

        return result;
    }

    public async Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Attributes).ThenInclude(a => a.AttributeDefinition)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return product is null ? null : mapper.Map<ProductDto>(product);
    }

    public async Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(Guid productId, CancellationToken ct = default)
    {
        var variants = await db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Size)
            .ThenBy(v => v.Colour)
            .ToListAsync(ct);

        return variants.Select(v => mapper.Map<ProductVariantDto>(v)).ToList();
    }

    public async Task<PagedResult<ReviewDto>> GetReviewsAsync(Guid productId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(r => mapper.Map<ReviewDto>(r)).ToList();
        return new PagedResult<ReviewDto>(dtos, total, page, pageSize);
    }

    public async Task<ReviewDto> CreateReviewAsync(Guid productId, Guid userId, string author, CreateReviewRequest req, CancellationToken ct = default)
    {
        var review = new StyleNest.Infrastructure.Entities.Catalog.Review
        {
            Id            = Guid.NewGuid(),
            ProductId     = productId,
            UserId        = userId,
            Author        = author,
            Rating        = req.Rating,
            Title         = req.Title,
            Body          = req.Body,
            // ENH-PDP-008 — store up to 4 photo URLs as a JSON array
            PhotoUrlsJson = System.Text.Json.JsonSerializer.Serialize(
                                req.PhotoUrls?.Take(4).ToList() ?? []),
        };

        db.Reviews.Add(review);

        // Update product aggregate rating
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is not null)
        {
            var allRatings = await db.Reviews
                .Where(r => r.ProductId == productId)
                .Select(r => r.Rating)
                .ToListAsync(ct);
            allRatings.Add(req.Rating);
            product.AverageRating = allRatings.Average();
            product.ReviewCount   = allRatings.Count;
        }

        await db.SaveChangesAsync(ct);
        return mapper.Map<ReviewDto>(review);
    }

    public async Task<IReadOnlyList<ProductDto>> GetRelatedProductsAsync(Guid productId, int limit, CancellationToken ct = default)
    {
        // Find the category of the current product
        var current = await db.Products
            .AsNoTracking()
            .Select(p => new { p.Id, p.CategoryId })
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (current is null) return [];

        var related = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .AsNoTracking()
            .Where(p => p.CategoryId == current.CategoryId && p.Id != productId && p.IsActive)
            .OrderByDescending(p => p.AverageRating)
            .Take(limit)
            .ToListAsync(ct);

        return related.Select(p => mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        const string key = "catalog:categories";
        var cached = await cache.GetAsync<List<CategoryDto>>(key, ct);
        if (cached is not null) return cached;

        var cats = await db.Categories.AsNoTracking().ToListAsync(ct);
        var dtos = cats.Select(c => mapper.Map<CategoryDto>(c)).ToList();
        await cache.SetAsync(key, dtos, CategoryTtl, ct);
        return dtos;
    }

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(CancellationToken ct = default)
    {
        const string key = "catalog:brands";
        var cached = await cache.GetAsync<List<BrandDto>>(key, ct);
        if (cached is not null) return cached;

        var brands = await db.Brands.AsNoTracking().ToListAsync(ct);
        var dtos = brands.Select(b => mapper.Map<BrandDto>(b)).ToList();
        await cache.SetAsync(key, dtos, BrandTtl, ct);
        return dtos;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest req, CancellationToken ct = default)
    {
        var product = new Product
        {
            Id              = Guid.NewGuid(),
            Name            = req.Name,
            Slug            = GenerateSlug(req.Name),
            Description     = req.Description,
            BasePrice       = req.Price,
            DiscountedPrice = req.SalePrice,
            BrandId         = req.BrandId,
            CategoryId      = req.CategoryId,
            IsActive        = true
        };

        product.Images = req.ImageUrls.Select((url, i) => new ProductImage
        {
            Id           = Guid.NewGuid(),
            ProductId    = product.Id,
            Url          = url,
            DisplayOrder = i,
            IsPrimary    = i == 0
        }).ToList();

        if (req.Attributes is { Count: > 0 })
        {
            product.Attributes = req.Attributes.Select(a => new StyleNest.Infrastructure.Entities.Catalog.ProductAttribute
            {
                Id                  = Guid.NewGuid(),
                ProductId           = product.Id,
                AttributeDefinitionId = a.AttributeId,
                Value               = a.Value
            }).ToList();
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        // Invalidate product list cache on any new product
        await cache.RemoveByPrefixAsync("catalog:products:", ct);

        await db.Entry(product).Reference(p => p.Brand).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.Category).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.Attributes)
            .Query().Include(a => a.AttributeDefinition).LoadAsync(ct);

        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest req, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Attributes).ThenInclude(a => a.AttributeDefinition)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null) return null;

        product.Name            = req.Name;
        product.Slug            = GenerateSlug(req.Name);
        product.Description     = req.Description;
        product.BasePrice       = req.Price;
        product.DiscountedPrice = req.SalePrice;
        product.IsActive        = req.IsActive;

        await db.SaveChangesAsync(ct);

        // Invalidate caches for this product and product lists
        await cache.RemoveByPrefixAsync("catalog:products:", ct);

        return mapper.Map<ProductDto>(product);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest req, CancellationToken ct = default)
    {
        var category = new Category
        {
            Id       = Guid.NewGuid(),
            Name     = req.Name,
            Slug     = GenerateSlug(req.Name),
            ParentId = req.ParentId,
            ImageUrl = req.ImageUrl
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        // Invalidate category cache
        await cache.RemoveAsync("catalog:categories", ct);

        return mapper.Map<CategoryDto>(category);
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandRequest req, CancellationToken ct = default)
    {
        var brand = new Brand
        {
            Id      = Guid.NewGuid(),
            Name    = req.Name,
            Slug    = GenerateSlug(req.Name),
            LogoUrl = req.LogoUrl
        };

        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);

        // Invalidate brands cache
        await cache.RemoveAsync("catalog:brands", ct);

        return mapper.Map<BrandDto>(brand);
    }

    public async Task<IReadOnlyList<AttributeDefinitionDto>> GetCategoryAttributesAsync(Guid categoryId, CancellationToken ct = default)
    {
        var attrs = await db.CategoryAttributes
            .Include(ca => ca.AttributeDefinition)
            .AsNoTracking()
            .Where(ca => ca.CategoryId == categoryId)
            .OrderBy(ca => ca.DisplayOrder)
            .Select(ca => ca.AttributeDefinition)
            .ToListAsync(ct);

        return attrs.Select(a => mapper.Map<AttributeDefinitionDto>(a)).ToList();
    }

    public async Task<IReadOnlyList<AttributeDefinitionDto>> GetAllAttributesAsync(CancellationToken ct = default)
    {
        var attrs = await db.AttributeDefinitions.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
        return attrs.Select(a => mapper.Map<AttributeDefinitionDto>(a)).ToList();
    }

    public async Task<AttributeDefinitionDto> CreateAttributeDefinitionAsync(CreateAttributeDefinitionRequest req, CancellationToken ct = default)
    {
        var attr = new StyleNest.Infrastructure.Entities.Catalog.AttributeDefinition
        {
            Id           = Guid.NewGuid(),
            Name         = req.Name,
            DisplayName  = req.DisplayName,
            DataType     = req.DataType,
            IsFilterable = req.IsFilterable,
            IsRequired   = req.IsRequired,
            AllowedValues = req.AllowedValues
        };

        db.AttributeDefinitions.Add(attr);
        await db.SaveChangesAsync(ct);
        return mapper.Map<AttributeDefinitionDto>(attr);
    }

    public async Task MapCategoryAttributeAsync(Guid categoryId, MapCategoryAttributeRequest req, CancellationToken ct = default)
    {
        var exists = await db.CategoryAttributes.AnyAsync(
            ca => ca.CategoryId == categoryId && ca.AttributeDefinitionId == req.AttributeId, ct);

        if (!exists)
        {
            db.CategoryAttributes.Add(new StyleNest.Infrastructure.Entities.Catalog.CategoryAttribute
            {
                Id                  = Guid.NewGuid(),
                CategoryId          = categoryId,
                AttributeDefinitionId = req.AttributeId,
                DisplayOrder        = req.DisplayOrder
            });
            await db.SaveChangesAsync(ct);
        }
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("/", "-");
        return $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
