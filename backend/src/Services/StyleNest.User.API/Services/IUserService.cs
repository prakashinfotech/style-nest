using StyleNest.SharedKernel.Domain;
using StyleNest.User.API.DTOs;

namespace StyleNest.User.API.Services;

public interface IUserService
{
    Task<Result<UserProfileResponseDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserProfileResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto dto, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AddressResponseDto>>> GetAddressesAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AddressResponseDto>> CreateAddressAsync(Guid userId, CreateAddressRequestDto dto, CancellationToken ct = default);
    Task<Result<AddressResponseDto>> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequestDto dto, CancellationToken ct = default);
    Task<Result> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default);
    Task<Result> DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<WishlistItemResponseDto>>> GetWishlistAsync(Guid userId, CancellationToken ct = default);
    Task<Result> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default);
    Task<Result> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default);
}
