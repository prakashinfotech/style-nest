namespace StyleNest.Media.API.Services;

/// <summary>
/// ENH-ADMIN-004 — Contract for server-side image resizing using SixLabors.ImageSharp.
/// </summary>
public interface IImageResizeService
{
    /// <summary>
    /// Produces three size variants from the original image stream:
    ///   thumb  — 300 × 300 px (max), JPEG quality 80
    ///   medium — 600 × 600 px (max), JPEG quality 85
    ///   large  — 1200 × 1200 px (max), JPEG quality 90
    ///
    /// Each returned MemoryStream is at position 0 and contains a JPEG-encoded image.
    /// The caller is responsible for disposing the streams.
    /// </summary>
    Task<ImageResizeResult> ResizeAsync(Stream original, CancellationToken ct = default);
}

public sealed record ImageResizeResult(
    MemoryStream Thumb,   // 300 px max side
    MemoryStream Medium,  // 600 px max side
    MemoryStream Large    // 1200 px max side
) : IDisposable
{
    public void Dispose()
    {
        Thumb.Dispose();
        Medium.Dispose();
        Large.Dispose();
    }
}
