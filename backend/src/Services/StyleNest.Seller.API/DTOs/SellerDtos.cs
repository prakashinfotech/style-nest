namespace StyleNest.Seller.API.DTOs;

public record SellerProfileDto(
    Guid Id,
    string StoreName,
    string Slug,
    string Description,
    string? LogoUrl,
    string? GstNumber,
    string? PanNumber,
    string Status,
    decimal CommissionRate,
    DateTime? ApprovedAt,
    DateTime CreatedAt
);

public record UpdateSellerProfileRequest(
    string StoreName,
    string Description,
    string? LogoUrl,
    string? GstNumber,
    string? PanNumber,
    string? BankAccountNumber,
    string? BankIfsc
);

public record SellerDashboardDto(
    int TotalProducts,
    int TotalOrders,
    int PendingOrders,
    decimal TotalRevenue,
    decimal MonthRevenue,
    int LowStockCount,
    IEnumerable<RecentOrderDto> RecentOrders
);

public record RecentOrderDto(
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string Status,
    DateTime CreatedAt
);

public record SellerAnalyticsDto(
    decimal TotalRevenue,
    decimal MonthRevenue,
    decimal WeekRevenue,
    int TotalOrders,
    int MonthOrders,
    IEnumerable<DailyRevenueDto> RevenueByDay,
    IEnumerable<TopProductDto> TopProducts
);

public record DailyRevenueDto(DateTime Date, decimal Revenue, int Orders);
public record TopProductDto(Guid ProductId, string Name, int UnitsSold, decimal Revenue);

public record SellerProductDto(
    Guid Id,
    string Name,
    string Slug,
    decimal BasePrice,
    decimal? DiscountedPrice,
    string CategoryName,
    string BrandName,
    int TotalStock,
    bool IsActive,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAt
);

public record CreateSellerProductRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    decimal? DiscountedPrice,
    Guid CategoryId,
    Guid BrandId,
    IEnumerable<CreateVariantRequest> Variants,
    IEnumerable<string> ImageUrls,
    IEnumerable<ProductAttributeRequest>? Attributes
);

public record CreateVariantRequest(
    string Sku,
    string Size,
    string Colour,
    int StockQuantity,
    decimal? PriceOverride
);

public record ProductAttributeRequest(Guid AttributeDefinitionId, string Value);

public record UpdateSellerProductRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    decimal? DiscountedPrice,
    bool IsActive,
    IEnumerable<ProductAttributeRequest>? Attributes
);

public record InventoryItemDto(
    Guid InventoryId,
    Guid ProductVariantId,
    string Sku,
    string Size,
    string Colour,
    int Stock,
    decimal Price,
    int LowStockThreshold,
    bool IsLowStock
);

public record UpdateInventoryRequest(int Stock, decimal? Price, int? LowStockThreshold);

public record SellerOrderDto(
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IEnumerable<SellerOrderItemDto> Items
);

public record SellerOrderItemDto(
    Guid ProductId,
    string ProductName,
    string Variant,
    int Quantity,
    decimal UnitPrice
);

public record UpdateOrderStatusRequest(string Status, string? Notes);

public record PayoutDto(
    Guid Id,
    decimal Amount,
    string Status,
    string? TransactionReference,
    DateTime? ProcessedAt,
    string? Notes,
    DateTime CreatedAt
);

public record SellerApprovalRequest(Guid SellerId, bool Approve, string? RejectionReason);
