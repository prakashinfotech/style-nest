namespace StyleNest.Catalog.API.DTOs;

public record SellerProductDto(
    Guid                  Id,
    string                Name,
    string                Slug,
    string                Description,
    decimal               Price,
    decimal?              Mrp,
    Guid                  BrandId,
    string                BrandName,
    Guid                  CategoryId,
    string                CategoryName,
    IReadOnlyList<string> ImageUrls,
    bool                  InStock,
    bool                  IsActive,
    DateTime              CreatedAt
);

public record CreateSellerProductRequest(
    string                Name,
    string                Description,
    Guid                  BrandId,
    Guid                  CategoryId,
    decimal               Price,
    decimal?              Mrp,
    IReadOnlyList<string> ImageUrls
);

public record UpdateSellerProductRequest(
    string   Name,
    string   Description,
    decimal  Price,
    decimal? Mrp,
    bool     IsActive
);
