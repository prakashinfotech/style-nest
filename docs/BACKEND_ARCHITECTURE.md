# BACKEND_ARCHITECTURE.md — .NET Microservices Architecture
> Detailed internal architecture for each .NET 10 microservice.
> All services follow Clean Architecture. All run on ASP.NET Core 10.

---

## 1. Service Map

| Service | Port | Primary Entities | Key Dependencies |
|---|---|---|---|
| Gateway.API | 5000 | — | YARP, Redis (rate limit) |
| Auth.API | 5001 | ApplicationUser, RefreshToken, OtpCode | Identity, JWT |
| User.API | 5002 | User profile, Address, Wallet, Notification | Auth.API (JWT verify) |
| Catalog.API | 5003 | Product, Category, Brand, Attribute, Review | Media.API, Redis |
| Cart.API | 5004 | Cart, CartItem, Coupon | Catalog.API, Redis |
| Order.API | 5005 | Order, OrderItem, Return | Cart.API, Seller.API |
| Admin.API | 5009 | Banner, Coupon, CmsPage, AuditLog | All services |
| Seller.API | 5010 | Seller, SellerInventory, SellerPayout | Catalog.API, Order.API |
| Media.API | 5011 | MediaFile | MinIO/Azure Blob, Hangfire |

---

## 2. Gateway.API — YARP Configuration

```yaml
# appsettings.json — YARP routes
ReverseProxy:
  Routes:
    auth-route:
      ClusterId: auth-cluster
      Match:
        Path: /api/v1/auth/{**catch-all}
    user-route:
      ClusterId: user-cluster
      Match:
        Path: /api/v1/users/{**catch-all}
    catalog-route:
      ClusterId: catalog-cluster
      Match:
        Path: /api/v1/products/{**catch-all}
    # ... etc

  Clusters:
    auth-cluster:
      Destinations:
        default:
          Address: http://auth-api:5001
    # ... etc
```

**Gateway responsibilities:**
- Route requests to microservices by path prefix
- JWT signature pre-validation (reject malformed tokens before hitting services)
- Rate limiting (100 req/min on `/api/v1/auth/*`, 1000 req/min elsewhere)
- CORS policy enforcement
- Request/response logging with correlation IDs

---

## 3. Auth.API — Authentication Service

### Folder Structure

```
StyleNest.Auth.API/
├── Controllers/V1/
│   └── AuthController.cs         ← register, login, refresh, logout, verify-otp
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs            ← Identity + JWT orchestration
│   ├── ITokenService.cs
│   └── TokenService.cs           ← JWT RS256 issuance + refresh token generation
├── DTOs/
│   ├── Requests/
│   │   ├── RegisterRequestDto.cs
│   │   ├── LoginRequestDto.cs
│   │   ├── RefreshTokenRequestDto.cs
│   │   └── VerifyOtpRequestDto.cs
│   └── Responses/
│       └── AuthResponseDto.cs
├── Validators/
│   ├── RegisterValidator.cs
│   └── LoginValidator.cs
└── Mapping/
    └── AuthMappingProfile.cs
```

### Key Logic

```csharp
// TokenService.cs — RS256 JWT issuance
public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email!),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    foreach (var role in roles)
        claims.Add(new Claim(ClaimTypes.Role, role));

    // Seller gets sellerId claim
    if (roles.Contains("Seller"))
        claims.Add(new Claim("sellerId", user.SellerId.ToString()!));

    var key = new RsaSecurityKey(_rsa);
    var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    var token = new JwtSecurityToken(
        issuer:   _config.Issuer,
        audience: _config.Audience,
        claims:   claims,
        expires:  DateTime.UtcNow.AddMinutes(15),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/auth/register` | No | Register new customer account |
| POST | `/api/v1/auth/login` | No | Login, receive JWT + refresh token |
| POST | `/api/v1/auth/refresh` | No | Exchange refresh token for new access token |
| POST | `/api/v1/auth/logout` | Yes | Revoke refresh token |
| POST | `/api/v1/auth/forgot-password` | No | Send password reset OTP |
| POST | `/api/v1/auth/verify-otp` | No | Verify OTP for password reset or email confirm |
| POST | `/api/v1/auth/reset-password` | No | Reset password after OTP verify |
| POST | `/api/v1/auth/admin/create-admin` | SuperAdmin | Create admin account |
| POST | `/api/v1/auth/admin/create-seller` | AdminOrAbove | Create seller account |

---

## 4. User.API — User Management Service

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/users/me` | Auth | Get own profile |
| PUT | `/api/v1/users/me` | Auth | Update profile |
| GET | `/api/v1/users/me/addresses` | Auth | List saved addresses |
| POST | `/api/v1/users/me/addresses` | Auth | Add address |
| PUT | `/api/v1/users/me/addresses/{id}` | Auth | Update address |
| DELETE | `/api/v1/users/me/addresses/{id}` | Auth | Delete address |
| POST | `/api/v1/users/me/addresses/{id}/set-default` | Auth | Set default address |
| GET | `/api/v1/users/me/wishlist` | Auth | Get wishlist |
| POST | `/api/v1/users/me/wishlist/{productId}` | Auth | Add to wishlist |
| DELETE | `/api/v1/users/me/wishlist/{productId}` | Auth | Remove from wishlist |
| GET | `/api/v1/users/me/wallet` | Auth | Get wallet balance + transactions |
| POST | `/api/v1/users/me/wallet/add-money` | Auth | Add money to wallet |
| GET | `/api/v1/users/me/notifications` | Auth | List notifications |
| POST | `/api/v1/users/me/notifications/{id}/read` | Auth | Mark notification as read |
| POST | `/api/v1/users/me/notifications/read-all` | Auth | Mark all as read |
| GET | `/api/v1/users/{id}` | AdminOrAbove | Get user by ID (admin use) |
| GET | `/api/v1/users` | AdminOrAbove | List all users (paginated) |
| POST | `/api/v1/users/{id}/suspend` | AdminOrAbove | Suspend user account |

---

## 5. Catalog.API — Product Catalog Service

### Folder Structure (additional)

```
├── Controllers/V1/
│   ├── ProductsController.cs
│   ├── CategoriesController.cs
│   ├── BrandsController.cs
│   ├── AttributesController.cs   ← NEW: category attribute definitions
│   └── ReviewsController.cs
```

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/products` | No | Paginated product list with filters |
| GET | `/api/v1/products/{id}` | No | Get single product |
| GET | `/api/v1/products/{id}/variants` | No | Get all variants for a product |
| GET | `/api/v1/products/{id}/related` | No | Related products (same category) |
| GET | `/api/v1/products/{id}/reviews` | No | Paginated product reviews |
| POST | `/api/v1/products/{id}/reviews` | Auth (User) | Submit a review |
| GET | `/api/v1/categories` | No | Category tree |
| GET | `/api/v1/categories/{id}/attributes` | No | Attribute definitions for a category |
| POST | `/api/v1/categories` | AdminOrAbove | Create category |
| PUT | `/api/v1/categories/{id}` | AdminOrAbove | Update category |
| GET | `/api/v1/brands` | No | List all brands |
| POST | `/api/v1/brands` | AdminOrAbove | Create brand |
| PUT | `/api/v1/brands/{id}` | AdminOrAbove | Update brand |
| GET | `/api/v1/attributes` | SellerOrAbove | List all attribute definitions |
| POST | `/api/v1/attributes` | AdminOrAbove | Create attribute definition |
| POST | `/api/v1/categories/{id}/attributes` | AdminOrAbove | Map attribute to category |

### Dynamic Attribute Query

```csharp
// CatalogService.cs — filter by dynamic attributes
public async Task<PagedResult<ProductResponseDto>> GetProductsAsync(ProductQueryDto query)
{
    var q = _db.Products
        .AsNoTracking()
        .Where(p => p.IsActive && p.IsApproved)
        .Where(p => !query.CategoryId.HasValue || p.CategoryId == query.CategoryId)
        .Where(p => !query.BrandId.HasValue || p.BrandId == query.BrandId)
        .Where(p => !query.MinPrice.HasValue || p.BasePrice >= query.MinPrice)
        .Where(p => !query.MaxPrice.HasValue || p.BasePrice <= query.MaxPrice);

    // Dynamic attribute filtering
    if (query.Attributes?.Any() == true)
    {
        foreach (var (attrName, values) in query.Attributes)
        {
            var attrId = await _db.AttributeDefinitions
                .Where(a => a.Name == attrName)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();

            q = q.Where(p => p.ProductAttributes.Any(
                pa => pa.AttributeId == attrId && values.Contains(pa.Value)));
        }
    }

    return await q.ToPagedResultAsync(query.Page, query.PageSize);
}
```

---

## 6. Cart.API — Shopping Cart Service

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/cart` | Auth | Get current cart |
| POST | `/api/v1/cart/items` | Auth | Add item to cart |
| PUT | `/api/v1/cart/items/{id}` | Auth | Update quantity |
| DELETE | `/api/v1/cart/items/{id}` | Auth | Remove item |
| DELETE | `/api/v1/cart` | Auth | Clear entire cart |
| POST | `/api/v1/cart/coupon` | Auth | Apply coupon code |
| DELETE | `/api/v1/cart/coupon` | Auth | Remove applied coupon |
| POST | `/api/v1/cart/items/{id}/save-for-later` | Auth | Move to saved-for-later |
| GET | `/api/v1/cart/saved` | Auth | Get saved-for-later items |
| POST | `/api/v1/cart/saved/{id}/move-to-cart` | Auth | Move back to cart |

### Inventory Reservation Logic

```csharp
// CartService.cs — Atomic inventory reservation
public async Task AddItemAsync(string userId, AddCartItemRequestDto dto)
{
    using var transaction = await _db.Database.BeginTransactionAsync();
    try
    {
        var inventory = await _db.SellerInventory
            .Where(i => i.VariantId == dto.VariantId)
            .FirstOrThrowAsync();

        var available = inventory.Stock - inventory.Reserved;
        if (available < dto.Quantity)
            throw new BusinessException("Insufficient stock");

        // Reserve stock
        inventory.Reserved += dto.Quantity;

        // Add cart item
        var cart = await GetOrCreateCartAsync(userId);
        _db.CartItems.Add(new CartItem { ... });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch { await transaction.RollbackAsync(); throw; }
}
```

---

## 7. Order.API — Order Lifecycle Service

### Order State Machine

```
Placed → Confirmed → Processing → Shipped → OutForDelivery → Delivered
   ↓          ↓
Cancelled  Cancelled  (can cancel before Shipped)
                                                ↓
                                          Return Requested → Return Approved → Refunded
```

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/orders` | Auth | Place order from cart |
| POST | `/api/v1/orders/buy-now` | Auth | Buy single item directly |
| GET | `/api/v1/orders` | Auth | User's order history (paginated) |
| GET | `/api/v1/orders/{id}` | Auth | Order detail |
| GET | `/api/v1/orders/{id}/tracking` | Auth | Order tracking + status history |
| POST | `/api/v1/orders/{id}/cancel` | Auth | Cancel order (before Shipped) |
| POST | `/api/v1/orders/{id}/items/{itemId}/return` | Auth | Request item return |
| GET | `/api/v1/seller/orders` | Seller | Seller's incoming orders |
| PUT | `/api/v1/seller/orders/{id}/status` | Seller | Update order status |
| GET | `/api/v1/admin/orders` | AdminOrAbove | All platform orders |
| PUT | `/api/v1/admin/orders/{id}/status` | AdminOrAbove | Force update any order status |
| POST | `/api/v1/admin/orders/{id}/approve-return` | AdminOrAbove | Approve return + trigger refund |

---

## 8. Admin.API — Administration Service

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/admin/dashboard` | AdminOrAbove | Dashboard analytics summary |
| GET | `/api/v1/admin/analytics/revenue` | AdminOrAbove | Revenue chart data |
| GET | `/api/v1/admin/analytics/orders` | AdminOrAbove | Orders analytics |
| GET | `/api/v1/admin/analytics/sellers` | AdminOrAbove | Seller performance |
| GET | `/api/v1/admin/analytics/products` | AdminOrAbove | Product performance |
| GET | `/api/v1/admin/banners` | AdminOrAbove | List banners |
| POST | `/api/v1/admin/banners` | AdminOrAbove | Create banner |
| PUT | `/api/v1/admin/banners/{id}` | AdminOrAbove | Update banner |
| DELETE | `/api/v1/admin/banners/{id}` | AdminOrAbove | Delete banner |
| GET | `/api/v1/admin/coupons` | AdminOrAbove | List coupons |
| POST | `/api/v1/admin/coupons` | AdminOrAbove | Create coupon |
| PUT | `/api/v1/admin/coupons/{id}` | AdminOrAbove | Update coupon |
| DELETE | `/api/v1/admin/coupons/{id}` | AdminOrAbove | Delete coupon |
| GET | `/api/v1/admin/cms` | AdminOrAbove | List CMS pages |
| POST | `/api/v1/admin/cms` | AdminOrAbove | Create CMS page |
| PUT | `/api/v1/admin/cms/{id}` | AdminOrAbove | Update CMS page |
| GET | `/api/v1/super-admin/admins` | SuperAdminOnly | List admin users |
| POST | `/api/v1/super-admin/admins` | SuperAdminOnly | Create admin account |
| POST | `/api/v1/super-admin/admins/{id}/suspend` | SuperAdminOnly | Suspend admin |
| GET | `/api/v1/super-admin/audit-logs` | SuperAdminOnly | Platform audit log |
| GET | `/api/v1/super-admin/rbac` | SuperAdminOnly | Role permission matrix |
| PUT | `/api/v1/super-admin/rbac` | SuperAdminOnly | Update role permissions |

---

## 9. Seller.API — Seller Management Service

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/seller/profile` | Seller | Get own seller profile |
| PUT | `/api/v1/seller/profile` | Seller | Update store profile |
| GET | `/api/v1/seller/dashboard` | Seller | Sales + revenue summary |
| GET | `/api/v1/seller/analytics` | Seller | Detailed seller analytics |
| GET | `/api/v1/seller/products` | Seller | Own product list |
| POST | `/api/v1/seller/products` | Seller | Create product (with dynamic attributes) |
| PUT | `/api/v1/seller/products/{id}` | Seller | Update product |
| DELETE | `/api/v1/seller/products/{id}` | Seller | Delete (soft) product |
| GET | `/api/v1/seller/inventory` | Seller | Inventory overview |
| PUT | `/api/v1/seller/inventory/{variantId}` | Seller | Update stock quantity |
| GET | `/api/v1/seller/orders` | Seller | Incoming orders |
| PUT | `/api/v1/seller/orders/{id}/status` | Seller | Update order status |
| GET | `/api/v1/seller/payouts` | Seller | Payout history |
| GET | `/api/v1/admin/sellers` | AdminOrAbove | List all sellers |
| GET | `/api/v1/admin/sellers/{id}` | AdminOrAbove | Seller detail |
| POST | `/api/v1/admin/sellers/{id}/approve` | AdminOrAbove | Approve seller |
| POST | `/api/v1/admin/sellers/{id}/suspend` | AdminOrAbove | Suspend seller |

---

## 10. Media.API — File Upload Service

### Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/media/upload` | SellerOrAbove | Upload image (multipart/form-data) |
| POST | `/api/v1/media/upload-video` | SellerOrAbove | Upload product video |
| GET | `/api/v1/media/{id}` | SellerOrAbove | Get media file metadata |
| DELETE | `/api/v1/media/{id}` | SellerOrAbove | Delete media file |

### Upload Pipeline

```csharp
// MediaService.cs
public async Task<MediaFileResponseDto> UploadImageAsync(IFormFile file, string userId)
{
    // 1. Validate MIME type
    var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
    if (!allowedTypes.Contains(file.ContentType))
        throw new ValidationException("Invalid file type");

    // 2. Validate size (10MB max)
    if (file.Length > 10 * 1024 * 1024)
        throw new ValidationException("File too large");

    // 3. Generate unique storage path
    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    var storagePath = $"products/original/{fileName}";

    // 4. Upload to MinIO / Azure Blob
    await _storageService.UploadAsync(storagePath, file.OpenReadStream(), file.ContentType);

    // 5. Insert MediaFiles record
    var mediaFile = new MediaFile { FileName = fileName, StoragePath = storagePath, ... };
    await _db.MediaFiles.AddAsync(mediaFile);
    await _db.SaveChangesAsync();

    // 6. Enqueue resize job (async)
    _backgroundJobs.Enqueue<ResizeImageJob>(job => job.ExecuteAsync(mediaFile.Id));

    return _mapper.Map<MediaFileResponseDto>(mediaFile);
}
```

---

## 11. Shared Infrastructure

### Global Exception Middleware

```csharp
// GlobalExceptionMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    try { await _next(context); }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = 422;
        await context.Response.WriteAsJsonAsync(new ProblemDetails {
            Status = 422,
            Title = "Validation Failed",
            Detail = ex.Message,
            Extensions = { ["errors"] = ex.Errors }
        });
    }
    catch (NotFoundException ex)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new ProblemDetails {
            Status = 404, Title = "Not Found", Detail = ex.Message
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new ProblemDetails {
            Status = 500, Title = "Internal Server Error"
        });
    }
}
```

### IRepository Pattern

```csharp
// IRepository<T>.cs
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);       // Soft delete — sets IsDeleted = true
    IQueryable<T> Query();           // For complex queries in services
}
```

### Hangfire Background Jobs

```csharp
// Registered in Admin.API or dedicated Worker
services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
services.AddHangfireServer();

// Recurring jobs
RecurringJob.AddOrUpdate<CartAbandonmentJob>("cart-cleanup",
    job => job.ExecuteAsync(), Cron.Every(30).Minutes());
RecurringJob.AddOrUpdate<DailyAnalyticsJob>("daily-analytics",
    job => job.ExecuteAsync(), Cron.Daily(0));
RecurringJob.AddOrUpdate<ExpireCouponsJob>("expire-coupons",
    job => job.ExecuteAsync(), Cron.Hourly());
RecurringJob.AddOrUpdate<LowStockAlertJob>("low-stock",
    job => job.ExecuteAsync(), Cron.Hourly());
```

---

## 12. Health Checks

Every service exposes `/health` returning:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": { "status": "Healthy", "duration": "00:00:00.0050000" },
    "redis":    { "status": "Healthy", "duration": "00:00:00.0020000" }
  }
}
```

```csharp
// Program.cs (every service)
builder.Services
    .AddHealthChecks()
    .AddSqlServer(connectionString, name: "database")
    .AddRedis(redisConnection, name: "redis");

app.MapHealthChecks("/health");
```

---

*See [ARCHITECTURE.md](ARCHITECTURE.md) for system-level decisions and sequence diagrams.*
*See [DATABASE_SCHEMA.md](DATABASE_SCHEMA.md) for full table definitions.*
