namespace StyleNest.Cart.API.DTOs;

public record CartItemDto(
    Guid   Id,
    Guid   ProductVariantId,
    Guid   ProductId,
    string ProductName,
    string? ImageUrl,
    string Size,
    string? Colour,
    decimal UnitPrice,
    int    Quantity,
    decimal TotalPrice
);

public record CartDto(
    Guid   Id,
    IReadOnlyList<CartItemDto> Items,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal Total,
    string? CouponCode
);
