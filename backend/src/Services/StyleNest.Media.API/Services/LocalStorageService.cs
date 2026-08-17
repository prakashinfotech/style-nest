namespace StyleNest.Media.API.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalStorageService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _basePath = configuration["LocalStorage:BasePath"]
                    ?? Path.Combine(env.ContentRootPath, "uploads");
        _baseUrl = configuration["LocalStorage:BaseUrl"] ?? "http://localhost:5011/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Storage key '{storageKey}' not found on local disk.", fullPath);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var folder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var key = $"{folder}/{Guid.NewGuid()}/{fileName}";
        var fullPath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs, cancellationToken);
        return key;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storageKey)
        => $"{_baseUrl}/{storageKey.Replace(Path.DirectorySeparatorChar, '/')}";
}
