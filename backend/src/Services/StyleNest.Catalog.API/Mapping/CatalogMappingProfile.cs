using AutoMapper;
using System.Text.Json;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Infrastructure.Entities.Catalog;

namespace StyleNest.Catalog.API.Mapping;

public sealed class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<ProductVariant, ProductVariantDto>()
            .ConstructUsing((v, _) => new ProductVariantDto(
                v.Id, v.Size, v.Colour, v.StockQuantity, v.PriceOverride
            ));

        CreateMap<ProductAttribute, ProductAttributeDto>()
            .ConstructUsing((a, _) => new ProductAttributeDto(
                a.AttributeDefinitionId,
                a.AttributeDefinition?.Name ?? string.Empty,
                a.AttributeDefinition?.DisplayName ?? string.Empty,
                a.Value
            ));

        CreateMap<AttributeDefinition, AttributeDefinitionDto>()
            .ConstructUsing((a, _) => new AttributeDefinitionDto(
                a.Id, a.Name, a.DisplayName, a.DataType,
                a.IsFilterable, a.IsRequired, a.AllowedValues
            ));

        CreateMap<Product, ProductDto>()
            .ConstructUsing((p, ctx) => new ProductDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Description ?? string.Empty,
                p.BasePrice,
                p.DiscountedPrice,
                p.BrandId,
                p.Brand?.Name ?? string.Empty,
                p.CategoryId,
                p.Category?.Name ?? string.Empty,
                p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).ToList(),
                p.Variants.Select(v => ctx.Mapper.Map<ProductVariantDto>(v)).ToList(),
                p.Attributes.Select(a => ctx.Mapper.Map<ProductAttributeDto>(a)).ToList(),
                p.AverageRating,
                p.ReviewCount,
                p.IsActive,
                p.Has360View      // ENH-PDP-007
            ));

        CreateMap<Category, CategoryDto>()
            .ConstructUsing((c, _) => new CategoryDto(
                c.Id, c.Name, c.Slug, c.ParentId, c.ImageUrl
            ));

        CreateMap<Brand, BrandDto>()
            .ConstructUsing((b, _) => new BrandDto(
                b.Id, b.Name, b.Slug, b.LogoUrl
            ));

        CreateMap<Review, ReviewDto>()
            .ConstructUsing((r, _) => new ReviewDto(
                r.Id, r.ProductId, r.UserId, r.Author,
                r.Rating, r.Title, r.Body, r.CreatedAt,
                // ENH-PDP-008 — deserialise photo URL array from JSON storage
                JsonSerializer.Deserialize<List<string>>(r.PhotoUrlsJson ?? "[]") ?? []
            ));
    }
}
