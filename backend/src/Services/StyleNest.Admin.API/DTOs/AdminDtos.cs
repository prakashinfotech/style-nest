namespace StyleNest.Admin.API.DTOs;

public record AdminOrderDto(
    Guid     Id,
    string   OrderNumber,
    string   UserEmail,
    decimal  TotalAmount,
    string   Status,
    DateTime CreatedAt,
    int      ItemCount);

public record AdminUserDto(
    Guid                   Id,
    string                 Email,
    string                 FirstName,
    string                 LastName,
    IReadOnlyList<string>  Roles,
    bool                   EmailConfirmed,
    DateTime               CreatedAt);

public record AdminProductDto(
    Guid     Id,
    string   Name,
    string   BrandName,
    string   CategoryName,
    decimal  Price,
    bool     InStock,
    bool     IsActive,
    DateTime CreatedAt);

public record CreateSellerRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public record CreateSellerResponse(
    Guid   Id,
    string Email,
    string FirstName,
    string LastName);

public record UpdateOrderStatusRequest(string Status);

public record UpdateProductStatusRequest(bool IsActive);

public record DashboardMetricsDto(
    int     TotalOrders,
    decimal TotalRevenue,
    int     TotalUsers,
    int     TotalProducts,
    int     TotalSellers,
    int     PendingSellers,
    int     TotalBrands,
    int     TotalCategories);

public record RevenueDataDto(
    DateTime Date,
    decimal  Revenue,
    int      OrderCount);

public record SellerSummaryDto(
    Guid    Id,
    string  StoreName,
    string  Email,
    string  Status,
    decimal CommissionRate,
    int     ProductCount,
    DateTime CreatedAt);

public record ApproveSellersRequest(bool Approve, string? RejectionReason);

public record AdminSummaryDto(
    Guid   Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime CreatedAt);

public record CreateAdminUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);
