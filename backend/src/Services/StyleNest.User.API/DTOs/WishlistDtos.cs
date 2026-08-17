namespace StyleNest.User.API.DTOs;

public record WishlistItemResponseDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    decimal BasePrice,
    decimal? DiscountedPrice,
    string? PrimaryImageUrl,
    string BrandName,
    DateTime AddedAt);
