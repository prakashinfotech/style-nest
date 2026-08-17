using StyleNest.Catalog.API.DTOs;

namespace StyleNest.Catalog.API.Services;

/// <summary>
/// ENH-SRCH-001 — Azure Cognitive Search (and DB-backed search) warm-up.
///
/// Fires 10 representative search queries immediately after the application starts
/// (with a short delay to let the DI container and database connectivity settle).
/// This prevents cold-start latency on the first real user request by:
///   1. Pre-populating the Redis distributed cache with common search results
///   2. Warming JIT-compiled EF Core query plans in the SQL engine
///   3. (Post-ENH-CAT-006) Pre-loading Azure Cognitive Search result caches
///
/// Configuration (appsettings.json):
/// <code>
/// "SearchWarmUp": {
///   "Enabled":       true,
///   "DelaySeconds":  30,
///   "Queries":       []   // optional override; empty uses the built-in 10 queries
/// }
/// </code>
///
/// The warm-up is non-blocking: all failures are swallowed and logged at Warning
/// so that a bad search query never prevents the application from starting.
/// </summary>
public sealed class SearchWarmUpBackgroundService(
    IServiceScopeFactory     scopeFactory,
    IConfiguration           configuration,
    ILogger<SearchWarmUpBackgroundService> logger) : BackgroundService
{
    // ── Default top-10 fashion search queries (representative of production traffic) ──
    private static readonly IReadOnlyList<string> DefaultWarmUpQueries =
    [
        "men kurta",
        "women saree",
        "formal shoes",
        "denim jeans",
        "ethnic wear",
        "party dress",
        "sneakers",
        "handbag",
        "sunglasses",
        "watch",
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var section = configuration.GetSection("SearchWarmUp");
        var enabled = section.GetValue<bool>("Enabled", defaultValue: true);

        if (!enabled)
        {
            logger.LogInformation("ENH-SRCH-001: Search warm-up is disabled via config.");
            return;
        }

        var delaySeconds = section.GetValue<int>("DelaySeconds", defaultValue: 30);
        logger.LogInformation(
            "ENH-SRCH-001: Search warm-up scheduled in {Delay}s after startup.", delaySeconds);

        // ── Wait for DB + Redis to be ready before firing ──────────────────
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return; // App shutting down before warm-up started
        }

        // ── Resolve queries (config override or defaults) ──────────────────
        var configuredQueries = section.GetSection("Queries").Get<string[]>();
        var queries = (configuredQueries is { Length: > 0 })
            ? (IReadOnlyList<string>)configuredQueries
            : DefaultWarmUpQueries;

        logger.LogInformation(
            "ENH-SRCH-001: Starting search warm-up — firing {Count} queries.", queries.Count);

        var succeeded = 0;
        var failed    = 0;

        foreach (var query in queries)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await using var scope   = scopeFactory.CreateAsyncScope();
                var catalogService      = scope.ServiceProvider.GetRequiredService<ICatalogService>();

                _ = await catalogService.GetProductsAsync(
                    new ProductQueryDto { Search = query, PageSize = 10 },
                    stoppingToken);

                succeeded++;
                logger.LogDebug("ENH-SRCH-001: Warm-up query '{Query}' — OK", query);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                logger.LogWarning(ex, "ENH-SRCH-001: Warm-up query '{Query}' failed — skipping", query);
            }

            // Small pause between queries to avoid thundering-herd on first boot
            await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "ENH-SRCH-001: Search warm-up complete — {Succeeded} succeeded, {Failed} failed.",
            succeeded, failed);
    }
}
