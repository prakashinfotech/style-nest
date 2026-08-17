using AutoMapper;
using StyleNest.Infrastructure.Entities.Media;
using StyleNest.Media.API.DTOs;

namespace StyleNest.Media.API.Mapping;

public class MediaMappingProfile : Profile
{
    public MediaMappingProfile()
    {
        CreateMap<MediaFile, MediaDto>()
            .ConstructUsing(src => new MediaDto(
                src.Id,
                src.FileName,
                src.OriginalFileName,
                src.ContentType,
                src.SizeBytes,
                src.StorageUrl,
                src.ThumbnailUrl,
                src.Type.ToString(),
                src.AltText,
                src.CreatedAt
            ));
    }
}
