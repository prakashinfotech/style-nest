# SECURITY.md — Security Architecture
> Security design, threat model, and implementation checklist.
> Defense-in-depth approach: gateway → API → database layers.

---

## 1. Threat Model

| Threat | Mitigation |
|---|---|
| Token forgery | JWT RS256 — private key only in Auth.API |
| XSS cookie theft | RefreshToken in HttpOnly, SameSite=Strict cookie |
| CSRF | SameSite=Strict cookie; no state-changing GET endpoints |
| SQL injection | EF Core parameterized queries only — zero raw SQL |
| Mass assignment | DTOs + AutoMapper — entities never exposed directly |
| Broken access control | Server-side RBAC policies on every endpoint |
| IDOR (Insecure Direct Object Reference) | Seller ownership requirement handler; userId from JWT only |
| Brute force | Rate limiting on auth endpoints (100 req/min per IP) |
| Replay attacks | JTI-based token blacklist in Redis on logout |
| File upload attacks | MIME whitelist, size limits, content-type validation |
| Info leakage | ProblemDetails hides stack traces in non-Development |

---

## 2. Authentication Security

### JWT Configuration

```csharp
// All APIs — appsettings.json
{
  "Jwt": {
    "PublicKeyPath": "./keys/public.pem",
    "Issuer":   "https://auth.yourdomain.com",
    "Audience": "fashion-spa",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}

// Auth.API only — also needs:
{
  "Jwt": {
    "PrivateKeyPath": "./keys/private.pem"
  }
}
```

### Token Storage Rules

```
accessToken:   NgRx memory ONLY
               ✗ Never localStorage (XSS-accessible)
               ✗ Never sessionStorage
               Lost on page refresh → auto-refreshed via HttpOnly cookie

refreshToken:  HttpOnly cookie
               SameSite = Strict (blocks CSRF)
               Secure = true (HTTPS only in production)
               Path = /api/v1/auth/refresh (limits exposure)
               Domain = api.yourdomain.com (production)
```

### Token Blacklist (Logout)

```csharp
// On logout — Redis blacklist with TTL
public async Task LogoutAsync(string jti, int remainingSeconds)
{
    var key = $"blacklist:jti:{jti}";
    await _redis.SetStringAsync(key, "1",
        new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(remainingSeconds)
        });
}

// On every protected request — check blacklist
public async Task<bool> IsTokenRevokedAsync(string jti)
{
    return await _redis.GetStringAsync($"blacklist:jti:{jti}") != null;
}
```

### OTP Security

```
OTP specification:
  Length:    6 digits
  Expiry:    5 minutes
  Max uses:  1 (IsUsed = true after first verification)
  Rate limit: 3 OTP requests per 15 minutes per user (Redis counter)
  Delivery:  Email via MailKit (not SMS in V1)
```

---

## 3. Authorization Security

### Defense in Depth

```
Layer 1 — YARP Gateway:
  Reject malformed JWT (missing/invalid signature)
  Rate limit by IP

Layer 2 — ASP.NET Core Authorization Middleware:
  [Authorize(Policy = "...")] on every controller/action
  No public endpoint unless explicitly [AllowAnonymous]

Layer 3 — Service Layer:
  Seller ownership validation (sellerId from JWT, not request body)
  User ownership validation (userId from JWT claim)

Layer 4 — Database:
  Row-level filters — IsDeleted = false global query filter
  Seller products filtered by SellerId in every query

Rule: Frontend guards are UX-only. Server-side is the ONLY security boundary.
```

### Dangerous Patterns to Avoid

```csharp
// ✗ WRONG — Never trust userId from request body
public async Task<IActionResult> GetOrder([FromBody] GetOrderDto dto)
{
    var order = await _service.GetOrderAsync(dto.UserId, dto.OrderId);
    // dto.UserId can be spoofed by any user
}

// ✓ CORRECT — Always extract userId from JWT claim
public async Task<IActionResult> GetOrder(Guid orderId)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
    var order = await _service.GetOrderAsync(userId, orderId);
    if (order == null) return NotFound();
    // Ownership enforced in service layer
}
```

---

## 4. API Security

### Rate Limiting (YARP Gateway)

```csharp
// Gateway.API — Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth-policy", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 100;
        config.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("general-policy", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 1000;
    });
});

// Apply to routes
app.MapReverseProxy(config =>
{
    config.UseRateLimiting("auth-policy"); // for /api/v1/auth/*
});
```

### CORS Configuration

```csharp
// Strict CORS — no wildcard in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        var origins = env.IsDevelopment()
            ? new[] { "http://localhost:4200", "http://localhost:4201" }
            : new[] { "https://www.yourdomain.com", "https://admin.yourdomain.com" };

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();   // Required for HttpOnly cookie
    });
});
```

### Security Headers

```csharp
// Every API — response headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";
    if (!env.IsDevelopment())
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    await next();
});
```

---

## 5. Input Validation

All request DTOs have a corresponding FluentValidation validator. Zero `[FromBody]` parameters without a validator.

```csharp
// Example — CreateProductRequestValidator.cs
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequestDto>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(300).WithMessage("Title too long");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Price must be positive")
            .LessThan(1_000_000).WithMessage("Price unreasonably large");

        RuleFor(x => x.MRP)
            .GreaterThanOrEqualTo(x => x.BasePrice)
            .WithMessage("MRP must be >= selling price");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");
    }
}
```

---

## 6. Data Security

### Password Storage

ASP.NET Core Identity uses `PasswordHasher<TUser>` which implements:
- PBKDF2 with HMACSHA512
- 256-bit salt
- 100,000 iterations (Identity default — matches NIST 2024 guidance)

### PII in JWT

```
Allowed in JWT:
  ✓ sub (userId GUID)
  ✓ email
  ✓ role
  ✓ sellerId (GUID only, if Seller)
  ✓ jti (for blacklisting)

NOT in JWT:
  ✗ Full name
  ✗ Phone number
  ✗ Address
  ✗ Payment info
  ✗ Any financial data
```

### Soft Delete (PII retention)

Customer data uses `IsDeleted` soft delete flag. Hard deletion is not performed immediately to maintain order history integrity. A data retention policy purge job can be added for GDPR compliance in Phase 15.

---

## 7. File Upload Security

```csharp
// MediaService.cs — upload validation
private static readonly string[] AllowedImageTypes = {
    "image/jpeg", "image/png", "image/webp"
};
private static readonly string[] AllowedVideoTypes = {
    "video/mp4", "video/webm"
};

public void ValidateFile(IFormFile file, bool isVideo = false)
{
    var allowedTypes = isVideo ? AllowedVideoTypes : AllowedImageTypes;
    var maxSize = isVideo ? 500L * 1024 * 1024 : 10L * 1024 * 1024;

    // 1. Check declared Content-Type
    if (!allowedTypes.Contains(file.ContentType))
        throw new ValidationException($"File type {file.ContentType} not allowed");

    // 2. Check actual magic bytes (not just extension)
    using var stream = file.OpenReadStream();
    var header = new byte[8];
    stream.Read(header, 0, 8);

    if (!IsValidImageBytes(header) && !isVideo)
        throw new ValidationException("File content does not match declared type");

    // 3. Check size
    if (file.Length > maxSize)
        throw new ValidationException($"File size exceeds {maxSize / 1024 / 1024}MB limit");

    // 4. Sanitize filename (no path traversal)
    var safeFileName = Path.GetFileName(file.FileName);
    if (safeFileName != file.FileName)
        throw new ValidationException("Invalid filename");
}
```

---

## 8. Audit Logging

All SuperAdmin and Admin actions are logged to `[admin].AuditLogs`:

```csharp
// AuditService.cs — called from relevant service methods
public async Task LogAsync(string userId, string action, string resource,
    string resourceId, object? oldValues, object? newValues)
{
    await _db.AuditLogs.AddAsync(new AuditLog {
        UserId = userId,
        Action = action,
        Resource = resource,
        ResourceId = resourceId,
        OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
        NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
        IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent
    });
    await _db.SaveChangesAsync();
}

// Usage in AdminService
await _auditService.LogAsync(adminId, "ProductApproved", "Product", productId,
    before: new { product.IsApproved },
    after: new { IsApproved = true });
```

---

## 9. Production Security Checklist

Before first production deployment:

- [ ] HTTPS enforced on all App Service instances (HSTS enabled)
- [ ] RSA private key stored in Azure Key Vault (not in files)
- [ ] All connection strings in Azure Key Vault
- [ ] MinIO replaced with Azure Blob Storage (no self-hosted credentials in prod)
- [ ] Seq removed from production compose (use Application Insights)
- [ ] Rate limiting tuned for production traffic levels
- [ ] CORS origins set to production domains only
- [ ] SQL Server firewall allows only App Service IPs
- [ ] Redis auth password set (not open)
- [ ] Docker images scanned for vulnerabilities (Azure Defender for Containers)
- [ ] Application Insights alerting configured (5xx rate > 1%)
- [ ] Admin panel behind VPN or IP allowlist (optional but recommended)
- [ ] `ASPNETCORE_ENVIRONMENT=Production` set on all App Services
- [ ] Swagger UI disabled in production (`if (env.IsDevelopment()) app.UseSwaggerUI()`)

---

*See [DEPLOYMENT.md](DEPLOYMENT.md) for infrastructure setup.*
*See [ROLES_RBAC.md](ROLES_RBAC.md) for authorization design.*
