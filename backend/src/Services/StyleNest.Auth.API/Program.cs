using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Azure.Communication.Email;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StyleNest.Auth.API.Filters;
using StyleNest.Auth.API.Options;
using StyleNest.Auth.API.Services;
using StyleNest.Auth.API.Validators;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.Extensions;
using StyleNest.SharedKernel.HealthChecks;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "Auth.API"));

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
           .AddInterceptors(new SaveChangesAuditInterceptor()));

    // Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opt =>
    {
        opt.Password.RequireDigit = true;
        opt.Password.RequiredLength = 8;
        opt.Password.RequireUppercase = true;
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail = true;
        // ENH-AUTH-005: 5 failures trigger lockout; default duration overridden by LockoutService doubling
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        opt.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // JWT RS256 — Polly retry + 15-min key cache (ENH-AUTH-007)
    builder.Services.AddResilientJwtBearer(builder.Configuration);
    builder.Services.AddAuthorization();

    // OTP delivery channel — config-driven: "Logging" (dev) or "AzureCommunication" (prod)
    var otpOptions = builder.Configuration
        .GetSection(OtpDeliveryOptions.SectionName)
        .Get<OtpDeliveryOptions>() ?? new OtpDeliveryOptions();
    builder.Services.AddSingleton(otpOptions);

    if (otpOptions.Provider == "AzureCommunication")
    {
        var acsConnStr = otpOptions.AzureCommunication?.ConnectionString
            ?? throw new InvalidOperationException("OtpDelivery:AzureCommunication:ConnectionString not configured.");
        builder.Services.AddSingleton(new EmailClient(acsConnStr));
        builder.Services.AddScoped<IOtpDeliveryChannel, AzureCommunicationOtpDeliveryChannel>();
    }
    else if (otpOptions.Provider == "Smtp")
    {
        // ENH-NOTIF-004: MailKit SMTP + Hangfire background delivery
        var smtpOpts = otpOptions.Smtp ?? new SmtpEmailOptions();
        builder.Services.AddSingleton(smtpOpts);
        builder.Services.AddScoped<ISmtpMailSender, MailKitSmtpSender>();
        builder.Services.AddScoped<OtpEmailJob>();
        builder.Services.AddScoped<IOtpDeliveryChannel, HangfireOtpDeliveryChannel>();

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddHangfireServer(opt => opt.Queues = ["otp-delivery", "default"]);
    }
    else
    {
        builder.Services.AddScoped<IOtpDeliveryChannel, LoggingOtpDeliveryChannel>();
    }

    // SMS delivery channel — dev: logging; replace with ACS SMS / MSG91 in production
    builder.Services.AddScoped<ISmsDeliveryChannel, LoggingSmsDeliveryChannel>();

    // IMemoryCache — used by AccountMergeService for merge-token TTL (ENH-AUTH-003)
    builder.Services.AddMemoryCache();

    // IHttpContextAccessor — used by AuthService to capture device info (ENH-AUTH-004)
    builder.Services.AddHttpContextAccessor();

    // App services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IOtpService, OtpService>();
    builder.Services.AddScoped<IAccountMergeService, AccountMergeService>();
    builder.Services.AddScoped<ISessionService, SessionService>();
    builder.Services.AddScoped<ILockoutService, LockoutService>();
    builder.Services.AddScoped<IMfaService, MfaService>();

    // ENH-AUTH-001 — Facebook OAuth 2.0 service + dedicated HttpClient
    builder.Services.AddHttpClient<IFacebookAuthService, FacebookAuthService>(client =>
    {
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // ENH-AUTH-002 — Apple Sign-In service + dedicated HttpClient (fetches Apple JWKS)
    builder.Services.AddHttpClient<IAppleAuthService, AppleAuthService>(client =>
    {
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    builder.Services.AddControllers();

    // OpenAPI / Swagger (Swashbuckle 10.x + ASP.NET Core OpenAPI)
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    // Response compression — Brotli + Gzip
    builder.Services.AddResponseCompression(opt =>
    {
        opt.EnableForHttps = true;
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    // CORS — configurable via AllowedOrigins env/config; fallback to localhost dev origins
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                         ?? ["http://localhost:4200", "http://localhost:4201"];
    builder.Services.AddCors(opt =>
        opt.AddDefaultPolicy(p =>
            p.WithOrigins(allowedOrigins)
             .AllowAnyHeader()
             .AllowAnyMethod()));

    // ENH-AUTH-010 — IP-based rate limiting: 5 OTP requests per IP per 1-hour sliding window.
    // Sliding window (6 segments × 10 min) smooths burst rather than allowing 5 at t=0 + 5 at t=1h.
    // QueueLimit=0 → reject immediately; no queuing to prevent slow-loris style holding.
    builder.Services.AddRateLimiter(opt =>
    {
        opt.AddPolicy("otp-per-ip", context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit         = 5,
                    Window              = TimeSpan.FromHours(1),
                    SegmentsPerWindow   = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit          = 0,
                }));

        opt.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ctx.HttpContext.Response.Headers.RetryAfter = "3600";
            ctx.HttpContext.Response.ContentType = "application/json";
            await ctx.HttpContext.Response.WriteAsJsonAsync(
                new { code = "OTP_RATE_LIMIT", message = "Too many OTP requests. Please try again in 1 hour." },
                ct);
        };
    });

    // Health checks — SQL Server connectivity
    var connString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddHealthChecks()
        .AddCheck("sqlserver", new DatabaseHealthCheck(connString), tags: ["db", "ready"]);

    var app = builder.Build();

    app.UseResponseCompression();
    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseSecurityHeaders();
    app.UseCorrelationId();
    app.UseW3CTracing(); // ENH-ADMIN-007
    app.UseExceptionMiddleware();

    // Seed roles, admin user, and catalog data on startup.
    // Retried because on a fresh SQL Server volume every service races to
    // create/migrate the same shared database — a transient "cannot open
    // database" (4060) or "database already exists" failure here is expected
    // and self-resolves once the winning service finishes creating it.
    using (var scope = app.Services.CreateScope())
    {
        var db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await StyleNest.Infrastructure.Persistence.DbSeeder.SeedAsync(db, userMgr, roleMgr);
                break;
            }
            catch (Exception seedEx) when (attempt < maxAttempts)
            {
                Log.Warning(seedEx, "Database seeding attempt {Attempt}/{MaxAttempts} failed — retrying in {Delay}s",
                    attempt, maxAttempts, attempt * 3);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
            }
            catch (Exception seedEx)
            {
                Log.Error(seedEx, "Database seeding failed after {MaxAttempts} attempts — API will continue without seed data",
                    maxAttempts);
            }
        }
    }

    // ENH-AUTH-010 rate limiter — IP-based, before authentication
    app.UseRateLimiter();

    // Authentication + authorisation must run before UseHangfireDashboard so that
    // HangfireAdminAuthorizationFilter can read httpContext.User (ENH-ADMIN-002).
    app.UseAuthentication();
    app.UseAuthorization();

    // ENH-ADMIN-002 — Hangfire Dashboard: admin-only, available in all environments.
    // Access requires Admin/SuperAdmin JWT role (or loopback for local dev convenience).
    if (otpOptions.Provider == "Smtp")
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAdminAuthorizationFilter()],
            // Back-link points at the admin SPA instead of the default root
            AppPath = "/admin",
        });
    }

    // Swagger only in non-production environments
    if (!app.Environment.IsProduction())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Auth API v1"));
    }

    app.MapHealthChecks("/health");
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Auth.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
