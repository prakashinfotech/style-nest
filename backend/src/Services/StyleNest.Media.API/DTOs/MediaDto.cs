namespace StyleNest.Media.API.DTOs;

public record MediaDto(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string StorageUrl,
    string? ThumbnailUrl,
    string Type,
    string? AltText,
    DateTime CreatedAt
);

public record UploadMediaRequest
{
    public string? AltText { get; init; }
}
