namespace StyleNest.Media.API.Services;

public interface IStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    /// <summary>ENH-ADMIN-004 — Download a file by its storage key so the resize job can read it.</summary>
    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    string GetPublicUrl(string storageKey);
}
