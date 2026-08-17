# MEDIA_UPLOAD.md — Media Upload Architecture
> File upload pipeline for product images, videos, and banners.
> Development: MinIO (Docker) · Production: Azure Blob Storage + Azure CDN

---

## 1. Architecture Overview

```
Client (Angular)
     │
     │  POST /api/v1/media/upload (multipart/form-data)
     ▼
Media.API :5011
  ├── Validate MIME type + file size
  ├── Generate unique filename (GUID + extension)
  ├── Upload original → MinIO / Azure Blob
  ├── Insert MediaFiles record (DB)
  ├── Enqueue Hangfire ResizeImageJob
  └── Return { mediaId, originalUrl, status: "Processing" }
     │
     ▼
Hangfire Background Worker
  ├── ResizeImageJob
  │   ├── Download original
  │   ├── Resize to 3 sizes (ImageSharp):
  │   │   ├── thumb:  150×150 (center crop)
  │   │   ├── card:   400×400 (center crop)
  │   │   └── full:   800×800 (fit, no crop)
  │   ├── Upload all 3 sizes to storage
  │   ├── Update MediaFiles record (thumbUrl, cardUrl, fullUrl, IsProcessed = true)
  │   └── Push SignalR notification: "image-processed" → client
  │
  └── VideoThumbnailJob (for video uploads)
      ├── Extract frame at 0 seconds (FFmpeg via Process.Start)
      ├── Upload thumbnail as JPEG
      └── Update MediaFiles.ThumbnailUrl
```

---

## 2. Storage Structure

### MinIO Buckets

```
products/
  {productId}/
    original/    → original upload (never modified)
    thumb/       → 150×150 JPEG
    card/        → 400×400 JPEG
    full/        → 800×800 JPEG
    video/       → original video file
    video-thumb/ → auto-generated video thumbnail

banners/
  {bannerId}/
    original/
    full/

avatars/
  {userId}/
    original/
    thumb/      → 150×150 (used in product listings, reviews)
```

### Azure Blob (Production) — Same folder structure

```
Container: fashion-media (private)
CDN endpoint: https://cdn.yourdomain.com/
  → maps to: https://fashionmedia.blob.core.windows.net/fashion-media/
```

---

## 3. Docker — MinIO Service

```yaml
# docker-compose.yml
minio:
  image: minio/minio:latest
  container_name: minio
  command: server /data --console-address ":9001"
  environment:
    MINIO_ROOT_USER: minioadmin
    MINIO_ROOT_PASSWORD: minioadmin123
  ports:
    - "9000:9000"     # API
    - "9001:9001"     # Console UI
  volumes:
    - minio_data:/data
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
    interval: 30s
    timeout: 10s
    retries: 3
```

Access MinIO console at: `http://localhost:9001`

---

## 4. IStorageService Interface

```csharp
// Shared abstraction — same code for MinIO and Azure Blob
public interface IStorageService
{
    Task UploadAsync(string path, Stream content, string contentType);
    Task<Stream> DownloadAsync(string path);
    Task DeleteAsync(string path);
    string GetPublicUrl(string path);
}

// MinIO implementation (dev)
public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3;  // MinIO uses S3 SDK
    private readonly string _bucketName;
    // ...
}

// Azure Blob implementation (prod)
public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobClient;
    // ...
}

// Swapped via DI based on environment:
if (env.IsDevelopment())
    services.AddSingleton<IStorageService, MinioStorageService>();
else
    services.AddSingleton<IStorageService, AzureBlobStorageService>();
```

---

## 5. ResizeImageJob (Hangfire)

```csharp
// ResizeImageJob.cs — SixLabors.ImageSharp
public class ResizeImageJob
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public async Task ExecuteAsync(Guid mediaFileId)
    {
        var mediaFile = await _db.MediaFiles.FindAsync(mediaFileId)
            ?? throw new NotFoundException($"MediaFile {mediaFileId} not found");

        using var originalStream = await _storage.DownloadAsync(mediaFile.StoragePath);
        using var image = await Image.LoadAsync(originalStream);

        var sizes = new[]
        {
            ("thumb", 150, 150),
            ("card",  400, 400),
            ("full",  800, 800),
        };

        foreach (var (sizeName, width, height) in sizes)
        {
            using var clone = image.Clone(ctx =>
                ctx.Resize(new ResizeOptions {
                    Size = new Size(width, height),
                    Mode = sizeName == "full" ? ResizeMode.Max : ResizeMode.Crop
                })
            );

            using var ms = new MemoryStream();
            await clone.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 85 });
            ms.Position = 0;

            var path = mediaFile.StoragePath.Replace("original/", $"{sizeName}/");
            await _storage.UploadAsync(path, ms, "image/jpeg");

            // Update DB field
            if (sizeName == "thumb") mediaFile.ThumbUrl = _storage.GetPublicUrl(path);
            if (sizeName == "card")  mediaFile.CardUrl  = _storage.GetPublicUrl(path);
            if (sizeName == "full")  mediaFile.FullUrl  = _storage.GetPublicUrl(path);
        }

        mediaFile.IsProcessed = true;
        await _db.SaveChangesAsync();
    }
}
```

---

## 6. Angular File Upload Component

```typescript
// file-upload.component.ts (Admin Panel shared)
@Component({
  selector: 'app-file-upload',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="upload-zone"
      [class.drag-over]="isDragOver()"
      (dragover)="onDragOver($event)"
      (dragleave)="isDragOver.set(false)"
      (drop)="onDrop($event)"
      (click)="fileInput.click()">

      <input #fileInput type="file" [accept]="accept" [multiple]="multiple"
             class="hidden" (change)="onFilesSelected($event)" />

      @if (previews().length > 0) {
        <div class="preview-grid">
          @for (preview of previews(); track preview.id) {
            <div class="relative">
              <img [src]="preview.url" class="preview-image" />
              @if (preview.uploading) {
                <div class="upload-overlay">
                  <span class="text-white text-xs">Uploading...</span>
                </div>
              }
              <button (click)="removePreview(preview.id)" class="remove-btn">×</button>
            </div>
          }
        </div>
      } @else {
        <div class="upload-prompt">
          <lucide-icon name="Upload" class="text-mid-gray" />
          <p>Drag & drop or click to upload</p>
          <p class="text-xs text-muted">{{ accept }} · Max {{ maxSizeMB }}MB</p>
        </div>
      }
    </div>
  `
})
export class FileUploadComponent {
  @Input() accept = 'image/jpeg,image/png,image/webp';
  @Input() multiple = true;
  @Input() maxSizeMB = 10;
  @Output() uploaded = new EventEmitter<UploadedFile[]>();

  isDragOver = signal(false);
  previews = signal<FilePreview[]>([]);

  private readonly mediaService = inject(MediaService);

  onFilesSelected(event: Event): void {
    const files = (event.target as HTMLInputElement).files;
    if (files) this.processFiles(Array.from(files));
  }

  private async processFiles(files: File[]): Promise<void> {
    for (const file of files) {
      const preview = { id: crypto.randomUUID(), url: URL.createObjectURL(file), uploading: true };
      this.previews.update(p => [...p, preview]);

      this.mediaService.uploadImage(file).subscribe({
        next: (result) => {
          this.previews.update(p =>
            p.map(pr => pr.id === preview.id ? { ...pr, uploading: false, mediaId: result.id } : pr)
          );
          this.uploaded.emit(this.getUploadedFiles());
        },
        error: () => this.previews.update(p => p.filter(pr => pr.id !== preview.id))
      });
    }
  }
}
```

---

## 7. Validation Rules

| Type | Allowed MIME | Max Size | Max Count |
|---|---|---|---|
| Product image | `image/jpeg`, `image/png`, `image/webp` | 10 MB | 10 per product |
| Product video | `video/mp4`, `video/webm` | 500 MB | 1 per product |
| Banner image | `image/jpeg`, `image/png` | 5 MB | 1 per banner |
| Avatar | `image/jpeg`, `image/png` | 2 MB | 1 per user |

---

## 8. CDN Strategy

```
Development:
  MinIO direct URL: http://localhost:9000/products/thumb/{filename}.jpg

Staging:
  Azure Blob: https://staging-media.blob.core.windows.net/fashion-media/...

Production:
  Azure CDN: https://cdn.yourdomain.com/products/thumb/{filename}.jpg
  (Azure CDN endpoint in front of Azure Blob Storage)
  Cache TTL:
    images: 30 days (Cache-Control: max-age=2592000, immutable)
    videos: 7 days
```

---

## 9. Lifecycle & Cleanup

```
Unlinked file cleanup (Hangfire recurring — daily):
  DELETE FROM MediaFiles WHERE IsProcessed = 0 AND CreatedAt < DATEADD(day, -1, GETUTCDATE())
  → Also delete from MinIO/Blob

Product deleted (soft delete):
  ProductImages + ProductVideos soft-deleted automatically via cascade
  Media files kept for 30 days (for potential recovery), then purged

MinIO lifecycle rule (dev):
  Auto-delete objects in products/ not updated in 7 days (prevents disk fill)
```

---

*See [BACKEND_ARCHITECTURE.md](BACKEND_ARCHITECTURE.md) for Media.API endpoint details.*
*See [DEPLOYMENT.md](DEPLOYMENT.md) for Docker MinIO configuration.*
