using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Media.API.Jobs;
using StyleNest.Media.API.Mapping;
using StyleNest.Media.API.Services;
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
           .Enrich.WithProperty("Service", "Media.API"));

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
           .AddInterceptors(new SaveChangesAuditInterceptor()));

    // Identity — for JWT user resolution
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opt =>
    {
        opt.Password.RequireDigit           = true;
        opt.Password.RequiredLength         = 8;
        opt.Password.RequireUppercase       = true;
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail         = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // JWT RS256 — Polly retry + 15-min key cache (ENH-AUTH-007)
    builder.Services.AddResilientJwtBearer(builder.Configuration);
    builder.Services.AddAuthorization();

    // Storage service — LocalStorageService in dev, MinioStorageService in prod
    var useLocalStorage = builder.Configuration.GetValue<bool>("UseLocalStorage", builder.Environment.IsDevelopment());
    if (useLocalStorage)
    {
        builder.Services.AddScoped<IStorageService, LocalStorageService>();
    }
    else
    {
        var minioConfig = builder.Configuration.GetSection("Minio");
        var minioEndpoint = minioConfig["Endpoint"] ?? "localhost:9000";
        var minioUser = minioConfig["RootUser"] ?? builder.Configuration["MINIO_ROOT_USER"] ?? "minioadmin";
        var minioPass = minioConfig["RootPassword"] ?? builder.Configuration["MINIO_ROOT_PASSWORD"] ?? "minioadmin";
        var useSSL = minioConfig.GetValue<bool>("UseSSL", false);

        builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            new BasicAWSCredentials(minioUser, minioPass),
            new AmazonS3Config
            {
                ServiceURL = $"{(useSSL ? "https" : "http")}://{minioEndpoint}",
                ForcePathStyle = true,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName
            }));
        builder.Services.AddScoped<IStorageService, MinioStorageService>();
    }

    // Media service
    builder.Services.AddScoped<IMediaService, MediaService>();

    // ENH-ADMIN-004 — Hangfire: image resize background job
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddHangfireServer(opt => opt.Queues = ["image-resize", "default"]);

    // ENH-ADMIN-004 — Image resize service (SixLabors.ImageSharp) + job class
    builder.Services.AddScoped<IImageResizeService, ImageResizeService>();
    builder.Services.AddScoped<ImageResizeJob>();

    // AutoMapper
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MediaMappingProfile>());

    // Multipart body limit — 200 MB for video uploads
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opt =>
    {
        opt.MultipartBodyLengthLimit = 200 * 1024 * 1024;
    });

    builder.Services.AddControllers();

    // OpenAPI / Swagger
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Media API", Version = "v1" });
    });

    // Response compression
    builder.Services.AddResponseCompression(opt =>
    {
        opt.EnableForHttps = true;
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    // CORS
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                         ?? ["http://localhost:4200", "http://localhost:4201"];
    builder.Services.AddCors(opt =>
        opt.AddDefaultPolicy(p =>
            p.WithOrigins(allowedOrigins)
             .AllowAnyHeader()
             .AllowAnyMethod()));

    // Health checks
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

    // Serve local uploads in dev
    if (useLocalStorage)
    {
        var uploadsPath = builder.Configuration["LocalStorage:BasePath"] ?? "uploads";
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                Path.GetFullPath(uploadsPath)),
            RequestPath = "/uploads"
        });
    }

    if (!app.Environment.IsProduction())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Media API v1"));
    }

    app.MapHealthChecks("/health");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Media.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
