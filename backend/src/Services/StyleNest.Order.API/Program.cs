using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Filters;
using StyleNest.Order.API.Services;
using StyleNest.Order.API.Validators;
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
           .Enrich.WithProperty("Service", "Order.API"));

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
           .AddInterceptors(new SaveChangesAuditInterceptor()));

    // JWT RS256 — Polly retry + 15-min key cache (ENH-AUTH-007)
    builder.Services.AddResilientJwtBearer(builder.Configuration);
    builder.Services.AddAuthorization();

    // Idempotency cache — ENH-INFRA-002: AAD MSI Redis when configured; plain conn str; in-memory fallback
    if (!builder.Services.AddAzureRedisCache(builder.Configuration))
        builder.Services.AddDistributedMemoryCache();
    builder.Services.AddScoped<IdempotencyFilter>();

    // ENH-PROMO-001 — StyleNest Cash cashback configuration
    builder.Services.Configure<CashbackSettings>(
        builder.Configuration.GetSection(CashbackSettings.Section));

    // ENH-PAY-001 — PayU failover gateway selector
    builder.Services.Configure<PaymentGatewaySettings>(
        builder.Configuration.GetSection(PaymentGatewaySettings.Section));
    builder.Services.AddHttpClient("gateway-health");
    builder.Services.AddScoped<IGatewayHealthCheck, HttpGatewayHealthCheck>();
    builder.Services.AddScoped<IPaymentGatewaySelector, PaymentGatewaySelector>();

    // ENH-ORD-005 — Shiprocket / Delhivery AWB + Tracking + NDR
    builder.Services.Configure<ShippingSettings>(
        builder.Configuration.GetSection(ShippingSettings.Section));
    builder.Services.AddScoped<IShippingProviderClient, MockShippingProviderClient>();
    builder.Services.AddScoped<IShippingService, ShippingService>();

    // ENH-PAY-007 — StyleNest Cash redemption with pessimistic wallet lock
    builder.Services.AddScoped<IWalletRedemptionService, WalletRedemptionService>();
    // ENH-PROMO-002 — StyleNest Cash expiry policy (12-month inactivity)
    builder.Services.AddScoped<IWalletExpiryService, WalletExpiryService>();
    // ENH-PAY-006 — Razorpay vault card token management (no PAN stored)
    builder.Services.AddScoped<ICardTokenService, CardTokenService>();

    // ENH-ORD-003 — Azure Service Bus session-affinity publisher (FIFO per orderId)
    builder.Services.AddSingleton<IOrderSessionBusService, OrderSessionBusService>();

    // App services
    builder.Services.AddScoped<ICashbackService, CashbackService>();
    builder.Services.AddScoped<ICheckoutAuthorizationService, CheckoutAuthorizationService>();
    builder.Services.AddScoped<IPaymentOptionsService, PaymentOptionsService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    // ENH-CHKOUT-002 — Express checkout (one-tap with saved address + saved card)
    builder.Services.AddScoped<IExpressCheckoutService, ExpressCheckoutService>();
    builder.Services.AddScoped<ISellerOrderService, SellerOrderService>();
    builder.Services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
    builder.Services.AddScoped<IPaymentReconciliationJob, PaymentReconciliationJob>();
    builder.Services.AddHostedService<PaymentReconciliationBackgroundService>();

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<PlaceOrderValidator>();

    builder.Services.AddControllers(o => o.Filters.AddService<IdempotencyFilter>());

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
        catch (Exception ex) { Log.Error(ex, "Order.API — migration failed, continuing"); }
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
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Order API v1"));
    }

    app.MapHealthChecks("/health");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
