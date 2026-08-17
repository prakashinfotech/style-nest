using StyleNest.Media.API.DTOs;

namespace StyleNest.Media.API.Services;

public interface IMediaService
{
    Task<MediaDto> UploadImageAsync(IFormFile file, string? altText, Guid uploadedBy, CancellationToken cancellationToken = default);
    Task<MediaDto> UploadVideoAsync(IFormFile file, string? altText, Guid uploadedBy, CancellationToken cancellationToken = default);
    Task<MediaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid requestedBy, CancellationToken cancellationToken = default);
}
