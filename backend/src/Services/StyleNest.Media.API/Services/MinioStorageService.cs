using Amazon.S3;
using Amazon.S3.Model;

namespace StyleNest.Media.API.Services;

public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _publicEndpoint;

    public MinioStorageService(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _bucket = configuration["Minio:Bucket"] ?? "stylenest-media";
        _publicEndpoint = configuration["Minio:PublicEndpoint"] ?? "http://localhost:9000";
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucket,
            Key        = storageKey,
        };
        var response = await _s3.GetObjectAsync(request, cancellationToken);
        // Copy to a MemoryStream so the S3 response connection can be closed.
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var key = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey
        };
        await _s3.DeleteObjectAsync(request, cancellationToken);
    }

    public string GetPublicUrl(string storageKey)
        => $"{_publicEndpoint}/{_bucket}/{storageKey}";
}
