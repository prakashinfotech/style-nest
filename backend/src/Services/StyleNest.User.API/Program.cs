using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.Extensions;
using StyleNest.SharedKernel.HealthChecks;
using StyleNest.User.API.Mapping;
using StyleNest.User.API.Services;
using StyleNest.User.API.Validators;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "User.API"));

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
           .AddInterceptors(new SaveChangesAuditInterceptor()));

    // Identity (read-only — auth is owned by Auth.API; needed for UserManager)
    builder.Services.AddIdentityCore<ApplicationUser>()
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>();

    // JWT RS256 — Polly retry + 15-min key cache (ENH-AUTH-007)
    builder.Services.AddResilientJwtBearer(builder.Configuration);
    builder.Services.AddAuthorization();

    // AutoMapper
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<UserMappingProfile>());

    // App services
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IWalletService, WalletService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IErasureService, ErasureService>();

    // Notification retry — ENH-NOTIF-001
    builder.Services.AddScoped<INotificationDlqSink, NullNotificationDlqSink>();
    builder.Services.AddScoped<INotificationRetryJob, NotificationRetryJob>();
    builder.Services.AddHostedService<NotificationRetryBackgroundService>();

    // ENH-NOTIF-003 — WhatsApp Business Channel (MSG91)
    // When MSG91 auth key is configured, WhatsApp sender replaces the null sender.
    // Otherwise, NullNotificationSender is used as fallback.
    var msg91Settings = builder.Configuration
        .GetSection(Msg91WhatsAppSettings.Section)
        .Get<Msg91WhatsAppSettings>() ?? new Msg91WhatsAppSettings();
    builder.Services.AddSingleton(msg91Settings);
    builder.Services.AddHttpClient("msg91-wa");
    builder.Services.AddScoped<IMsg91WhatsAppClient, Msg91WhatsAppClient>();
    if (!string.IsNullOrWhiteSpace(msg91Settings.AuthKey) && !msg91Settings.AuthKey.StartsWith("REPLACE"))
    {
        builder.Services.AddScoped<INotificationSender, WhatsAppNotificationSender>();
    }
    else
    {
        builder.Services.AddScoped<INotificationSender, NullNotificationSender>();
    }

    // ENH-NOTIF-005 — DLQ Depth Alert (>100 dead-lettered for >15min → Critical log → App Insights alert)
    builder.Services.AddSingleton<DlqAlertState>();
    builder.Services.AddScoped<IDlqDepthMonitor, DlqDepthMonitorJob>();
    builder.Services.AddHostedService<DlqDepthMonitorBackgroundService>();

    // ENH-NOTIF-002 — FCM Push Notifications
    builder.Services.Configure<FcmSettings>(
        builder.Configuration.GetSection(FcmSettings.Section));
    builder.Services.AddHttpClient("fcm");
    builder.Services.AddScoped<IFcmNotificationService, FcmNotificationService>();

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateProfileValidator>();

    builder.Services.AddControllers();
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
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "User API v1"));
    }

    app.MapHealthChecks("/health");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "User.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
