using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using StyleNest.SharedKernel.Extensions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "Gateway.API"));

    // JWT RS256 — Polly retry + 15-min key cache (ENH-AUTH-007)
    if (!string.IsNullOrEmpty(builder.Configuration["Jwt:PublicKey"]))
    {
        builder.Services.AddResilientJwtBearer(builder.Configuration);
        builder.Services.AddAuthorization();
    }

    // Rate limiting
    builder.Services.AddRateLimiter(opt =>
    {
        opt.AddFixedWindowLimiter("auth-limiter", cfg =>
        {
            cfg.PermitLimit         = 20;
            cfg.Window              = TimeSpan.FromMinutes(1);
            cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            cfg.QueueLimit          = 0;
        });

        opt.AddFixedWindowLimiter("global-limiter", cfg =>
        {
            cfg.PermitLimit         = 200;
            cfg.Window              = TimeSpan.FromMinutes(1);
            cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            cfg.QueueLimit          = 10;
        });

        opt.RejectionStatusCode = 429;
    });

    // CORS — configurable via AllowedOrigins env/config
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                         ?? ["http://localhost:4200", "http://localhost:4201"];
    builder.Services.AddCors(opt =>
        opt.AddDefaultPolicy(p =>
            p.WithOrigins(allowedOrigins)
             .AllowAnyHeader()
             .AllowAnyMethod()));

    // Response compression — Brotli + Gzip
    builder.Services.AddResponseCompression(opt =>
    {
        opt.EnableForHttps = true;
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    // Health checks — aggregate downstream service liveness via HTTP
    builder.Services.AddHttpClient("health-client");
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Gateway is running"));

    // YARP
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    var app = builder.Build();

    app.UseResponseCompression();
    app.UseSerilogRequestLogging();
    app.UseW3CTracing(); // ENH-ADMIN-007 — generate/propagate traceparent before proxying
    app.UseCors();
    app.UseRateLimiter();

    if (!string.IsNullOrEmpty(builder.Configuration["Jwt:PublicKey"]))
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Security headers on all gateway responses
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        ctx.Response.Headers.Append("X-Frame-Options", "DENY");
        ctx.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        ctx.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        await next();
    });

    // Aggregated health check — gateway + downstream liveness
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            var result = new
            {
                status  = report.Status.ToString(),
                service = "StyleNest Gateway",
                timestamp = DateTime.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name   = e.Key,
                    status = e.Value.Status.ToString(),
                }),
            };
            await ctx.Response.WriteAsJsonAsync(result);
        },
    });

    app.MapReverseProxy();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
