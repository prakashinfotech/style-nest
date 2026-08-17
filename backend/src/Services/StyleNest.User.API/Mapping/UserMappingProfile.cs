using AutoMapper;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Entities.Commerce;
using StyleNest.User.API.DTOs;

namespace StyleNest.User.API.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<ApplicationUser, UserProfileResponseDto>();

        CreateMap<UserAddress, AddressResponseDto>();

        CreateMap<CreateAddressRequestDto, UserAddress>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        CreateMap<WishlistItem, WishlistItemResponseDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Product.Id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductSlug, opt => opt.MapFrom(src => src.Product.Slug))
            .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => src.Product.BasePrice))
            .ForMember(dest => dest.DiscountedPrice, opt => opt.MapFrom(src => src.Product.DiscountedPrice))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Product.Brand.Name))
            .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                src.Product.Images.FirstOrDefault(i => i.IsPrimary) != null
                    ? src.Product.Images.First(i => i.IsPrimary).Url
                    : src.Product.Images.FirstOrDefault() != null
                        ? src.Product.Images.First().Url
                        : null))
            .ForMember(dest => dest.AddedAt, opt => opt.MapFrom(src => src.CreatedAt));
    }
}
