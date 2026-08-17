namespace StyleNest.Order.API.DTOs;

public record SellerOrderItemDto(
    Guid    ProductId,
    string  ProductName,
    int     Quantity,
    decimal UnitPrice
);

public record SellerOrderDto(
    Guid                          Id,
    string                        OrderNumber,
    string                        CustomerName,
    string                        Status,
    decimal                       TotalAmount,
    DateTime                      CreatedAt,
    IReadOnlyList<SellerOrderItemDto> Items
);
