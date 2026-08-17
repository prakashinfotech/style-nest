using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Media.API.Services;

namespace StyleNest.Media.API.Jobs;

/// <summary>
/// ENH-ADMIN-004 — Hangfire background job that resizes an uploaded image into three
/// standard variants (thumb 300px, medium 600px, large 1200px) using SixLabors.ImageSharp,
/// then stores each variant and stamps <see cref="StyleNest.Infrastructure.Entities.Media.MediaFile.ThumbnailUrl"/>
/// on the original record.
///
/// Enqueued by <see cref="MediaService.UploadImageAsync"/> immediately after the original
/// file is persisted.  Hangfire handles retries automatically (default: 10 attempts with
/// exponential back-off) so transient storage failures are self-healing.
/// </summary>
public sealed class ImageResizeJob(
    AppDbContext          db,
    IStorageService       storage,
    IImageResizeService   resizer,
    ILogger<ImageResizeJob> logger)
{
    /// <summary>Hangfire entry point — receives the primary key of the <c>MediaFile</c> to process.</summary>
    public async Task ExecuteAsync(Guid mediaId)
    {
        var media = await db.MediaFiles.FirstOrDefaultAsync(m => m.Id == mediaId);
        if (media is null)
        {
            logger.LogWarning("ENH-ADMIN-004 ImageResizeJob: MediaFile {Id} not found — skipping.", mediaId);
            return;
        }

        if (media.ContentType is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif"))
        {
            logger.LogInformation(
                "ENH-ADMIN-004 ImageResizeJob: MediaFile {Id} ContentType={CT} is not resizable — skipping.",
                mediaId, media.ContentType);
            return;
        }

        logger.LogInformation(
            "ENH-ADMIN-004 ImageResizeJob: resizing MediaFile {Id} ({OriginalKey})",
            mediaId, media.FileName);

        // ── Download original ────────────────────────────────────────────────
        await using var original = await storage.DownloadAsync(media.FileName);

        // ── Resize ──────────────────────────────────────────────────────────
        using var result = await resizer.ResizeAsync(original);

        // ── Upload variants ──────────────────────────────────────────────────
        var baseName = Path.GetFileNameWithoutExtension(media.FileName);
        var folder   = Path.GetDirectoryName(media.FileName)?.Replace('\\', '/') ?? string.Empty;

        string Build(string suffix) =>
            string.IsNullOrEmpty(folder) ? $"{baseName}_{suffix}.jpg"
                                         : $"{folder}/{baseName}_{suffix}.jpg";

        var thumbKey  = await storage.UploadAsync(result.Thumb,  Build("thumb"),  "image/jpeg");
        var mediumKey = await storage.UploadAsync(result.Medium, Build("medium"), "image/jpeg");
        var largeKey  = await storage.UploadAsync(result.Large,  Build("large"),  "image/jpeg");

        // ── Persist thumbnail URL on the original record ─────────────────────
        media.ThumbnailUrl = storage.GetPublicUrl(thumbKey);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "ENH-ADMIN-004 ImageResizeJob: MediaFile {Id} variants created — thumb={T} medium={M} large={L}",
            mediaId,
            storage.GetPublicUrl(thumbKey),
            storage.GetPublicUrl(mediumKey),
            storage.GetPublicUrl(largeKey));
    }
}
