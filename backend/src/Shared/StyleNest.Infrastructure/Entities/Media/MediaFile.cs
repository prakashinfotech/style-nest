using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Media;

public enum MediaType { Image, Video, Document }

public class MediaFile : BaseEntity<Guid>
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public Guid UploadedBy { get; set; }
    public MediaType Type { get; set; } = MediaType.Image;
    public string? AltText { get; set; }
}
