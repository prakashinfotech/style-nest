using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.Interceptors;
using StyleNest.Seller.API.Mapping;
using StyleNest.Seller.API.Services;
using StyleNest.Seller.API.Validators;
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
           .Enrich.WithProperty("Service", "Seller.API"));

    // ENH-SELL-001: scoped services needed before DbContext registration
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentSellerContext, HttpCurrentSellerContext>();
    builder.Services.AddScoped<SellerSessionContextInterceptor>();

    // DbContext — ENH-SELL-001: SellerSessionContextInterceptor injected per-scope
    builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
           .AddInterceptors(
               new SaveChangesAuditInterceptor(),
               sp.GetRequiredService<SellerSessionContextInterceptor>()));

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

    // App services — ISellerService
    builder.Services.AddScoped<ISellerService, SellerService>();
    // ENH-SELL-002 — KYC document submission and admin review workflow
    builder.Services.AddScoped<ISellerKycService, SellerKycService>();
    // ENH-SELL-003 — Seller Payout via Razorpay Payout API
    var rzpPayoutSettings = builder.Configuration
        .GetSection(RazorpayPayoutSettings.Section)
        .Get<RazorpayPayoutSettings>() ?? new RazorpayPayoutSettings();
    builder.Services.AddSingleton(rzpPayoutSettings);
    builder.Services.AddHttpClient("razorpay-payout");
    builder.Services.AddScoped<RazorpayPayoutClient>();
    builder.Services.AddScoped<ISellerPayoutService, SellerPayoutService>();

    // AutoMapper
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<SellerMappingProfile>());

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateSellerProfileValidator>();

    builder.Services.AddControllers();

    // OpenAPI / Swagger
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Seller API", Version = "v1" });
    });

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
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Seller API v1"));
    }

    app.MapHealthChecks("/health");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Seller.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
