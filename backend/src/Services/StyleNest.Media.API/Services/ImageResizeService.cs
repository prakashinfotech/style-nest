using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace StyleNest.Media.API.Services;

/// <summary>
/// ENH-ADMIN-004 — SixLabors.ImageSharp implementation of <see cref="IImageResizeService"/>.
///
/// Each variant is produced with <c>ResizeMode.Max</c> so the aspect ratio is preserved
/// and neither dimension exceeds the specified target size.
/// Output format is always JPEG for predictable file size and broad browser support.
/// </summary>
public sealed class ImageResizeService(ILogger<ImageResizeService> logger) : IImageResizeService
{
    private static readonly (int MaxSide, int Quality, string Label)[] Variants =
    [
        (300,  80, "thumb"),
        (600,  85, "medium"),
        (1200, 90, "large"),
    ];

    public async Task<ImageResizeResult> ResizeAsync(Stream original, CancellationToken ct = default)
    {
        // Load once, resize three times
        using var image = await Image.LoadAsync(original, ct);

        logger.LogDebug(
            "ImageResizeService: original {W}×{H}px, producing {Count} variants",
            image.Width, image.Height, Variants.Length);

        var streams = new MemoryStream[Variants.Length];
        for (var i = 0; i < Variants.Length; i++)
        {
            var (maxSide, quality, label) = Variants[i];
            streams[i] = await ResizeOneAsync(image, maxSide, quality, label, ct);
        }

        return new ImageResizeResult(streams[0], streams[1], streams[2]);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private static async Task<MemoryStream> ResizeOneAsync(
        Image image, int maxSide, int quality, string label, CancellationToken ct)
    {
        // Clone so the original is not mutated between variants
        using var clone = image.Clone(ctx =>
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(maxSide, maxSide),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3, // high-quality Lanczos
            }));

        var ms = new MemoryStream();
        var encoder = new JpegEncoder { Quality = quality };
        await clone.SaveAsJpegAsync(ms, encoder, ct);
        ms.Position = 0;
        return ms;
    }
}
