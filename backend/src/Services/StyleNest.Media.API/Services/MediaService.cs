using AutoMapper;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Media;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Media.API.DTOs;
using StyleNest.Media.API.Jobs;

namespace StyleNest.Media.API.Services;

public class MediaService : IMediaService
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private static readonly string[] AllowedVideoTypes = ["video/mp4", "video/webm", "video/quicktime"];
    private const long MaxImageBytes = 10 * 1024 * 1024;  // 10 MB
    private const long MaxVideoBytes = 200 * 1024 * 1024; // 200 MB

    // Magic bytes (file signatures) for MIME validation
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        { "image/jpeg", [0xFF, 0xD8, 0xFF] },
        { "image/png",  [0x89, 0x50, 0x4E, 0x47] },
        { "image/webp", [0x52, 0x49, 0x46, 0x46] }, // RIFF
        { "image/gif",  [0x47, 0x49, 0x46, 0x38] }, // GIF8
        { "video/mp4",  [0x00, 0x00, 0x00, 0x18] }, // ftyp box prefix
    };

    private readonly AppDbContext _db;
    private readonly IStorageService _storage;
    private readonly IMapper _mapper;
    private readonly IBackgroundJobClient _jobs;

    public MediaService(AppDbContext db, IStorageService storage, IMapper mapper, IBackgroundJobClient jobs)
    {
        _db = db;
        _storage = storage;
        _mapper = mapper;
        _jobs = jobs;
    }

    public async Task<MediaDto> UploadImageAsync(IFormFile file, string? altText, Guid uploadedBy, CancellationToken cancellationToken = default)
    {
        ValidateFile(file, AllowedImageTypes, MaxImageBytes);
        await ValidateMagicBytesAsync(file, cancellationToken);

        var fileName = SanitizeFileName(file.FileName);
        string storageKey;
        await using (var stream = file.OpenReadStream())
            storageKey = await _storage.UploadAsync(stream, fileName, file.ContentType, cancellationToken);

        var media = new MediaFile
        {
            Id = Guid.NewGuid(),
            FileName = storageKey,
            OriginalFileName = fileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StorageUrl = _storage.GetPublicUrl(storageKey),
            UploadedBy = uploadedBy,
            Type = MediaType.Image,
            AltText = altText
        };

        _db.MediaFiles.Add(media);
        await _db.SaveChangesAsync(cancellationToken);

        // ENH-ADMIN-004 — Enqueue image resize job (thumb/medium/large variants)
        _jobs.Enqueue<ImageResizeJob>(job => job.ExecuteAsync(media.Id));

        return _mapper.Map<MediaDto>(media);
    }

    public async Task<MediaDto> UploadVideoAsync(IFormFile file, string? altText, Guid uploadedBy, CancellationToken cancellationToken = default)
    {
        ValidateFile(file, AllowedVideoTypes, MaxVideoBytes);

        var fileName = SanitizeFileName(file.FileName);
        string storageKey;
        await using (var stream = file.OpenReadStream())
            storageKey = await _storage.UploadAsync(stream, fileName, file.ContentType, cancellationToken);

        var media = new MediaFile
        {
            Id = Guid.NewGuid(),
            FileName = storageKey,
            OriginalFileName = fileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StorageUrl = _storage.GetPublicUrl(storageKey),
            UploadedBy = uploadedBy,
            Type = MediaType.Video,
            AltText = altText
        };

        _db.MediaFiles.Add(media);
        await _db.SaveChangesAsync(cancellationToken);
        return _mapper.Map<MediaDto>(media);
    }

    public async Task<MediaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var media = await _db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return media is null ? null : _mapper.Map<MediaDto>(media);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid requestedBy, CancellationToken cancellationToken = default)
    {
        var media = await _db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (media is null) return false;

        await _storage.DeleteAsync(media.FileName, cancellationToken);
        media.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateFile(IFormFile file, string[] allowedTypes, long maxBytes)
    {
        if (file.Length == 0)
            throw new ArgumentException("File is empty.");
        if (file.Length > maxBytes)
            throw new ArgumentException($"File exceeds maximum size of {maxBytes / 1024 / 1024} MB.");
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new ArgumentException($"Content type '{file.ContentType}' is not allowed.");
    }

    private static async Task ValidateMagicBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (!MagicBytes.TryGetValue(file.ContentType.ToLower(), out var signature))
            return;

        var buffer = new byte[signature.Length];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, signature.Length), cancellationToken);

        if (read < signature.Length || !buffer.Take(read).SequenceEqual(signature))
            throw new ArgumentException("File content does not match the declared content type.");
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName).ToLower();
        var safe = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        return $"{safe}{ext}";
    }
}
