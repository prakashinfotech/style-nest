namespace StyleNest.Catalog.API.DTOs;

// ── Attribute DTOs ───────────────────────────────────────────────────────────

public record AttributeDefinitionDto(
    Guid   Id,
    string Name,
    string DisplayName,
    string DataType,
    bool   IsFilterable,
    bool   IsRequired,
    string? AllowedValues
);

public record CreateAttributeDefinitionRequest(
    string  Name,
    string  DisplayName,
    string  DataType,
    bool    IsFilterable,
    bool    IsRequired,
    string? AllowedValues
);

public record MapCategoryAttributeRequest(
    Guid AttributeId,
    int  DisplayOrder
);

public record ProductAttributeDto(
    Guid   AttributeId,
    string AttributeName,
    string AttributeDisplayName,
    string Value
);

public record ProductAttributeInput(
    Guid   AttributeId,
    string Value
);

// ── Read DTOs ────────────────────────────────────────────────────────────────

public record ProductVariantDto(
    Guid    Id,
    string  Size,
    string? Colour,
    int     StockQuantity,
    decimal? PriceOverride
);

public record ProductDto(
    Guid   Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? SalePrice,
    Guid   BrandId,
    string BrandName,
    Guid   CategoryId,
    string CategoryName,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductAttributeDto> Attributes,
    double Rating,
    int    ReviewCount,
    bool   InStock,
    /// <summary>ENH-PDP-007 — When true, the PDP renders a 360° drag-to-spin gallery.</summary>
    bool   Has360View = false
);

public record ProductListDto(
    IReadOnlyList<ProductDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record CategoryDto(
    Guid   Id,
    string Name,
    string Slug,
    Guid?  ParentId,
    string? ImageUrl
);

public record BrandDto(
    Guid   Id,
    string Name,
    string Slug,
    string? LogoUrl
);

// ── Write DTOs ───────────────────────────────────────────────────────────────

public record CreateProductRequest(
    string                Name,
    string                Description,
    Guid                  BrandId,
    Guid                  CategoryId,
    decimal               Price,
    decimal?              SalePrice,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<ProductAttributeInput>? Attributes = null
);

public record UpdateProductRequest(
    string   Name,
    string   Description,
    decimal  Price,
    decimal? SalePrice,
    bool     IsActive
);

public record CreateCategoryRequest(
    string  Name,
    Guid?   ParentId,
    string? ImageUrl
);

public record CreateBrandRequest(
    string  Name,
    string? LogoUrl
);
