using StyleNest.Seller.API.DTOs;
using StyleNest.SharedKernel.DTOs;

namespace StyleNest.Seller.API.Services;

public interface ISellerService
{
    Task<SellerProfileDto?> GetProfileAsync(Guid userId);
    Task<SellerProfileDto> UpdateProfileAsync(Guid userId, UpdateSellerProfileRequest request);
    Task<SellerDashboardDto> GetDashboardAsync(Guid sellerId);
    Task<SellerAnalyticsDto> GetAnalyticsAsync(Guid sellerId, int days = 30);

    Task<PagedResult<SellerProductDto>> GetProductsAsync(Guid sellerId, int page, int pageSize);
    Task<SellerProductDto> CreateProductAsync(Guid sellerId, CreateSellerProductRequest request);
    Task<SellerProductDto> UpdateProductAsync(Guid sellerId, Guid productId, UpdateSellerProductRequest request);
    Task DeleteProductAsync(Guid sellerId, Guid productId);

    Task<IEnumerable<InventoryItemDto>> GetInventoryAsync(Guid sellerId);
    Task<InventoryItemDto> UpdateInventoryAsync(Guid sellerId, Guid inventoryId, UpdateInventoryRequest request);

    Task<PagedResult<SellerOrderDto>> GetOrdersAsync(Guid sellerId, int page, int pageSize, string? status);
    Task<SellerOrderDto?> GetOrderAsync(Guid sellerId, Guid orderId);
    Task UpdateOrderStatusAsync(Guid sellerId, Guid orderId, UpdateOrderStatusRequest request);

    Task<PagedResult<PayoutDto>> GetPayoutsAsync(Guid sellerId, int page, int pageSize);
}
