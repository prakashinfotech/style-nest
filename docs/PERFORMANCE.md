# PERFORMANCE.md — Caching, Optimization & Performance Architecture
> Multi-layer performance strategy: database → API → CDN → Angular bundle.

---

## 1. Performance Targets

| Metric | Target | Measurement |
|---|---|---|
| API P99 latency | < 500ms | Application Insights |
| Product list page (cached) | < 50ms | Redis hit |
| Initial Angular bundle | < 200KB gzipped | `ng build --stats-json` |
| Time to Interactive (TTI) | < 3s on 3G | Lighthouse |
| Lighthouse Performance Score | > 90 | Production build |

---

## 2. Caching Architecture

### Layer 1 — Redis (Server-Side, Most Impactful)

```csharp
// CatalogService.cs — Redis caching pattern
public async Task<PagedResult<ProductResponseDto>> GetProductsAsync(ProductQueryDto query)
{
    var cacheKey = BuildCacheKey(query);

    // Check Redis first
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null)
        return JsonSerializer.Deserialize<PagedResult<ProductResponseDto>>(cached)!;

    // Cache miss — hit database
    var result = await FetchFromDbAsync(query);

    // Store in Redis with TTL
    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
        new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

    return result;
}

private string BuildCacheKey(ProductQueryDto query) =>
    $"catalog:products:page:{query.Page}:size:{query.PageSize}" +
    $":cat:{query.CategoryId}:brand:{query.BrandId}" +
    $":min:{query.MinPrice}:max:{query.MaxPrice}:sort:{query.SortBy}";
```

### Redis Cache Key Patterns

```
catalog:products:{page}:{size}:{filters-hash}    TTL: 10 min
catalog:product:{id}                              TTL: 30 min
catalog:categories:tree                           TTL: 60 min
catalog:brands:all                                TTL: 60 min
admin:banners:home                                TTL: 15 min
user:{userId}:cart                                TTL: 30 min (sliding)
user:{userId}:wishlist                            TTL: 30 min (sliding)
analytics:daily:{date}                            TTL: 24 hours
blacklist:jti:{jti}                               TTL: remaining token expiry
```

### Cache Invalidation Rules

```csharp
// Pattern: invalidate on write
public async Task UpdateProductAsync(Guid productId, UpdateProductRequestDto dto)
{
    await _repository.UpdateAsync(product);

    // Invalidate product cache
    await _cache.RemoveAsync($"catalog:product:{productId}");

    // Invalidate all product list pages for this category
    await InvalidateProductListCacheAsync(product.CategoryId);
}

private async Task InvalidateProductListCacheAsync(Guid categoryId)
{
    // Redis pattern delete — get matching keys and remove
    var server = _redis.GetServer(_redis.GetEndPoints()[0]);
    var keys = server.Keys(pattern: $"catalog:products:*:cat:{categoryId}*");
    foreach (var key in keys)
        await _cache.RemoveAsync(key);
}
```

### Layer 2 — Angular HTTP Cache

```typescript
// CachingInterceptor — browser-level short cache for read-only catalog data
export const cachingInterceptor: HttpInterceptorFn = (req, next) => {
  const cache = inject(HttpCacheService);

  if (req.method !== 'GET') return next(req);
  if (!isCacheable(req.url)) return next(req);

  const cached = cache.get(req.url);
  if (cached) return of(cached);

  return next(req).pipe(
    tap(response => {
      if (response instanceof HttpResponse) {
        cache.set(req.url, response, getTtl(req.url));
      }
    })
  );
};

const isCacheable = (url: string): boolean =>
  url.includes('/categories') || url.includes('/brands');

const getTtl = (url: string): number =>
  url.includes('/categories') ? 1800000 : 300000; // 30min or 5min
```

---

## 3. Database Query Optimization

### EF Core Best Practices

```csharp
// ✓ Always .AsNoTracking() for read-only queries
var products = await _db.Products
    .AsNoTracking()
    .Where(p => p.IsActive && p.IsApproved)
    .Select(p => new ProductSummaryDto {     // ✓ Project to DTO in DB
        Id = p.Id,
        Title = p.Title,
        BasePrice = p.BasePrice,
        // Only what's needed — no SELECT *
    })
    .ToListAsync();

// ✓ Avoid N+1 with Include or split query
var products = await _db.Products
    .AsNoTracking()
    .Include(p => p.ProductImages.Where(i => i.IsPrimary))
    .Include(p => p.Brand)
    .Include(p => p.Category)
    .ToListAsync();

// ✗ Never load full entities for list pages
var products = await _db.Products.ToListAsync(); // Loads all columns including Description
```

### Critical Indexes (Beyond FK Indexes)

```sql
-- Hot query patterns that need composite indexes

-- PLP filter: active, approved, by category
CREATE INDEX IX_Products_Category_Active_Approved
ON [catalog].Products (CategoryId, IsActive, IsApproved)
INCLUDE (Title, BasePrice, MRP, AverageRating);

-- PLP filter: by seller
CREATE INDEX IX_Products_Seller_Active
ON [catalog].Products (SellerId, IsActive)
INCLUDE (Title, BasePrice, IsApproved);

-- Price range filter
CREATE INDEX IX_Products_BasePrice
ON [catalog].Products (BasePrice)
WHERE IsActive = 1 AND IsApproved = 1;

-- Order history (user's orders)
CREATE INDEX IX_Orders_UserId_PlacedAt
ON [orders].Orders (UserId, PlacedAt DESC)
INCLUDE (OrderNumber, Status, Total);

-- Seller inventory lookups
CREATE INDEX IX_SellerInventory_VariantId
ON [seller].SellerInventory (VariantId)
INCLUDE (Stock, Reserved);

-- Notification bell (unread count)
CREATE INDEX IX_NotificationLogs_UserId_IsRead
ON [notifications].NotificationLogs (UserId, IsRead)
INCLUDE (SentAt);
```

### Pagination — Never Skip Large Offsets

```csharp
// ✓ Keyset pagination for large datasets (Phase 5+)
// ✗ OFFSET-based pagination has O(n) cost for large pages
// For now: OFFSET is acceptable up to ~50 pages (common catalog pattern)

var products = await _db.Products
    .AsNoTracking()
    .Where(p => p.IsActive && p.IsApproved)
    .OrderBy(p => p.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## 4. API Response Optimization

### Response Compression

```csharp
// All APIs — Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "application/problem+json" });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);

app.UseResponseCompression(); // Before everything else
```

### Output Caching (ASP.NET Core 7+)

```csharp
// For truly static endpoints like category tree
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("CatalogCache", builder =>
        builder.Expire(TimeSpan.FromMinutes(30))
               .Tag("catalog"));
});

[HttpGet("categories")]
[OutputCache(PolicyName = "CatalogCache")]
public async Task<IActionResult> GetCategories() { ... }

// Invalidate on update:
await _outputCacheStore.EvictByTagAsync("catalog", ct);
```

---

## 5. Angular Performance

### Bundle Size Control

```
Target: Initial bundle < 200KB gzipped

Strategies:
  ✓ All feature routes lazy-loaded (loadComponent/loadChildren)
  ✓ @defer (on viewport) for below-fold content
  ✓ Import individual icons: import { Heart } from 'lucide-angular'
     NOT: import * as icons from 'lucide-angular'
  ✓ PurgeCSS via Tailwind (removes unused utility classes in prod)
  ✓ Tree-shaking via esbuild (Angular CLI 17+ default)
  ✓ No moment.js — use date-fns (tree-shakeable)
```

### Change Detection

```typescript
// All components — OnPush mandatory
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush
})

// Use signals for local state — no unnecessary CD triggers
isOpen = signal(false);

// Use computed() for derived values — memoized
cartTotal = computed(() =>
  this.cartItems().reduce((sum, item) => sum + item.totalPrice, 0)
);

// Use toSignal() for store observables
products = toSignal(this.store.select(selectProducts), { initialValue: [] });
```

### Image Optimization

```html
<!-- Above-fold: eager loading (LCP critical) -->
<img [src]="heroImage.url" loading="eager" width="1440" height="480"
     fetchpriority="high" alt="Hero banner" />

<!-- Below-fold: lazy loading -->
<img [src]="product.imageUrl" loading="lazy" width="400" height="533"
     alt="{{ product.brand }} {{ product.title }}" />
```

```typescript
// Blur-up placeholder pattern
isImageLoaded = signal(false);
// Template:
// <div [class.hidden]="isImageLoaded()"><app-skeleton-loader /></div>
// <img (load)="isImageLoaded.set(true)" [class.opacity-0]="!isImageLoaded()" />
```

### Virtual Scrolling (Large Lists)

```typescript
// For search results with 200+ items
import { ScrollingModule } from '@angular/cdk/scrolling';

// Template
<cdk-virtual-scroll-viewport itemSize="400" class="product-viewport">
  <div *cdkVirtualFor="let product of products; trackBy: trackById">
    <app-product-card [product]="product" />
  </div>
</cdk-virtual-scroll-viewport>
```

---

## 6. Background Job Performance

### Hangfire Concurrency

```csharp
// Admin.API Program.cs — Hangfire server config
services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "critical", "default", "low" };
});

// Priority queues
[Queue("critical")]
public async Task SendOrderConfirmationAsync(Guid orderId) { ... }

[Queue("default")]
public async Task ResizeImageAsync(Guid mediaFileId) { ... }

[Queue("low")]
public async Task GenerateDailyAnalyticsAsync() { ... }
```

---

## 7. Monitoring & Alerting

### Application Insights KPIs

```
Alert rules (Production):
  5xx error rate > 1% over 5 min → PagerDuty
  P99 API latency > 2s → PagerDuty
  Redis cache hit rate < 60% → Warning email
  SQL Server DTU > 80% sustained → Warning email
  Hangfire queue depth > 500 → Warning email
```

### Correlation ID Tracking

```csharp
// CorrelationIdMiddleware.cs — propagated through all log entries
public async Task InvokeAsync(HttpContext context)
{
    var correlationId = context.Request.Headers["X-Correlation-Id"]
        .FirstOrDefault() ?? Guid.NewGuid().ToString();

    context.Response.Headers["X-Correlation-Id"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("UserId", context.User?.FindFirst("sub")?.Value))
    {
        await _next(context);
    }
}
```

### Structured Log Format

```json
{
  "Timestamp": "2026-05-15T10:30:00Z",
  "Level": "Information",
  "MessageTemplate": "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms",
  "Properties": {
    "Method": "GET",
    "Path": "/api/v1/products",
    "StatusCode": 200,
    "Elapsed": 45,
    "CorrelationId": "abc-123",
    "UserId": "guid-of-user",
    "Application": "catalog-api"
  }
}
```

---

## 8. Search Performance

### Phase 1 — EF Core Full-Text Search

```csharp
// SQL Server FULLTEXT INDEX on Products (Title, Description)
// Uses EF.Functions.Contains for basic full-text

var products = await _db.Products
    .Where(p => EF.Functions.Contains(p.Title, query.Search))
    .AsNoTracking()
    .ToListAsync();
```

### Phase 5+ — Azure Cognitive Search

```csharp
// Swap ISearchService implementation — no controller changes needed
public class AzureSearchService : ISearchService
{
    private readonly SearchClient _searchClient;

    public async Task<SearchResult<ProductIndexDoc>> SearchAsync(string query, SearchOptions opts)
    {
        return await _searchClient.SearchAsync<ProductIndexDoc>(query, opts);
    }
}
```

---

*See [DEPLOYMENT.md](DEPLOYMENT.md) for infrastructure configuration.*
*See [ARCHITECTURE.md](ARCHITECTURE.md) for system-level caching decisions.*
