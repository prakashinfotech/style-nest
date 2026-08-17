namespace StyleNest.Order.API.DTOs;

public record BuyNowRequest(
    Guid    ProductId,
    string? Size,
    string? Colour,
    int     Quantity
);
