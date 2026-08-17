using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StyleNest.Admin.API.Filters;
using StyleNest.Admin.API.Mapping;
using StyleNest.Admin.API.Services;
using StyleNest.Admin.API.Validators;
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
           .Enrich.WithProperty("Service", "Admin.API"));

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
           .AddInterceptors(new SaveChangesAuditInterceptor()));

    // Identity — UserManager needed for user listing and seller creation
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

    // ENH-AI-003 — Azure OpenAI Product Description Assistant (Admin CMS)
    var openAiSettings = builder.Configuration
        .GetSection(AzureOpenAiSettings.Section)
        .Get<AzureOpenAiSettings>() ?? new AzureOpenAiSettings();
    builder.Services.AddSingleton(openAiSettings);
    builder.Services.AddScoped<IProductDescriptionAssistant, ProductDescriptionAssistant>();

    // App services
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<ICouponStackingService, CouponStackingService>(); // ENH-PROMO-004
    // ENH-SRCH-003 — Search Synonyms Dictionary (Admin CMS management)
    builder.Services.AddScoped<ISearchSynonymService, SearchSynonymService>();

    // ENH-ADMIN-003 — Scheduled jobs (scoped per execution; also triggerable via API)
    builder.Services.AddScoped<DailyAnalyticsJob>();
    builder.Services.AddScoped<LowStockAlertJob>();
    builder.Services.AddScoped<CartAbandonmentJob>();
    builder.Services.AddScoped<ExpireCouponsJob>();
    // Background scheduler — runs each job on its own PeriodicTimer
    builder.Services.AddHostedService<JobSchedulerBackgroundService>();

    // AutoMapper
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AdminMappingProfile>());

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<CreateBannerValidator>();

    // ENH-AUTH-012: global MFA gate — 403 if mfa_verified claim absent on authenticated admin requests
    builder.Services.AddControllers(o => o.Filters.Add<RequireMfaFilter>());

    // OpenAPI / Swagger
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    // Response compression — Brotli + Gzip
    builder.Services.AddResponseCompression(opt =>
    {
        opt.EnableForHttps = true;
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        opt.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    // CORS — configurable via AllowedOrigins env/config
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

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    break;
                }
                catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1801)
                {
                    if (attempt < 5)
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }
        }
        catch (Exception ex) { Log.Error(ex, "Admin.API — migration failed, continuing"); }
    }

    app.UseResponseCompression();
    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseSecurityHeaders();
    app.UseCorrelationId();
    app.UseW3CTracing(); // ENH-ADMIN-007
    app.UseExceptionMiddleware();

    if (!app.Environment.IsProduction())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Admin API v1"));
    }

    app.MapHealthChecks("/health");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Admin.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
