namespace StyleNest.Order.API.DTOs;

public record OrderItemDto(
    Guid    Id,
    Guid    ProductId,
    string  ProductName,
    string? ImageUrl,
    string? VariantDetails,
    decimal UnitPrice,
    int     Quantity,
    decimal TotalPrice
);

public record OrderDto(
    Guid   Id,
    string OrderNumber,
    string Status,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal DeliveryCharge,
    decimal Total,
    string? CouponCode,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items
);
