/**
 * ENH-CAT-006 — Azure Cognitive Search Index Provisioner
 *
 * Background service that runs once on startup (after a short delay) to:
 *   1. Create or update the "stylenest-products" search index with correct schema
 *   2. Create or update the "stylenest-synonyms" synonym map from DB SearchSynonyms table
 *   3. Register a Suggester ("sg-products") on the Name field for autocomplete
 *
 * The service is a no-op when AzureCognitiveSearch:Endpoint is absent or starts
 * with "REPLACE" — local development works without Azure credentials.
 *
 * Note: Index updates are done with MergeOrUpload semantics — adding new fields
 * is safe; removing fields requires a full index rebuild (drop + recreate).
 */

using System.Text.Json;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

public sealed class SearchIndexInitializer(
    IServiceScopeFactory                    scopeFactory,
    AzureCognitiveSearchService             searchService,
    IConfiguration                          configuration,
    ILogger<SearchIndexInitializer>         logger) : BackgroundService
{
    private const int StartupDelaySeconds = 45;   // let DB migrations finish first

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var endpoint = configuration["AzureCognitiveSearch:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith("REPLACE"))
        {
            logger.LogInformation(
                "ENH-CAT-006: AzureCognitiveSearch:Endpoint not configured — index init skipped.");
            return;
        }

        // Short delay so migrations and DB seeding are complete
        try { await Task.Delay(TimeSpan.FromSeconds(StartupDelaySeconds), stoppingToken); }
        catch (OperationCanceledException) { return; }

        var indexClient = searchService.GetIndexClient();
        if (indexClient is null)
        {
            logger.LogWarning("ENH-CAT-006: SearchIndexClient is null — cannot provision index.");
            return;
        }

        try
        {
            await ProvisionSynonymMapAsync(indexClient, stoppingToken);
            await ProvisionIndexAsync(indexClient, stoppingToken);
            logger.LogInformation("ENH-CAT-006: Search index provisioning complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ENH-CAT-006: Search index provisioning failed — search will use DB fallback.");
        }
    }

    // ── Synonym Map ─────────────────────────────────────────────────────────

    private async Task ProvisionSynonymMapAsync(
        SearchIndexClient indexClient,
        CancellationToken ct)
    {
        var synonymMapName = configuration["AzureCognitiveSearch:SynonymMapName"]
                             ?? "stylenest-synonyms";

        var synonymRules = await BuildSynonymRulesAsync(ct);

        var synonymMap = new SynonymMap(synonymMapName, synonymRules);
        await indexClient.CreateOrUpdateSynonymMapAsync(synonymMap, cancellationToken: ct);

        logger.LogInformation(
            "ENH-CAT-006: Synonym map '{Name}' created/updated.", synonymMapName);
    }

    /// <summary>
    /// Loads synonyms from the SearchSynonyms DB table and adds a set of
    /// built-in fashion synonyms. Each line is: term, synonym1, synonym2, ...
    /// </summary>
    private async Task<string> BuildSynonymRulesAsync(CancellationToken ct)
    {
        var lines = new List<string>
        {
            // Built-in fashion synonyms
            "sneaker, trainer, sport shoe, sports shoe, running shoe",
            "kurta, kurti, kurthi, ethnic top",
            "saree, sari",
            "handbag, purse, clutch bag, tote bag",
            "denim, jeans, denims",
            "lehenga, lehnga, lehenga choli",
            "salwar, salwar kameez, salwar suit",
            "dupatta, stole, scarf",
            "churidar, churidar pant",
            "palazzo, palazzo pant",
        };

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var dbSynonyms = await db.SearchSynonyms
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var entry in dbSynonyms)
            {
                try
                {
                    var synonyms = JsonSerializer.Deserialize<string[]>(entry.SynonymsJson);
                    if (synonyms is { Length: > 0 })
                        lines.Add($"{entry.Term}, {string.Join(", ", synonyms)}");
                }
                catch
                {
                    // Ignore malformed synonym entries
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "ENH-CAT-006: Could not load DB synonyms — using built-in list only.");
        }

        return string.Join("\n", lines);
    }

    // ── Index ───────────────────────────────────────────────────────────────

    private async Task ProvisionIndexAsync(
        SearchIndexClient indexClient,
        CancellationToken ct)
    {
        var indexName      = configuration["AzureCognitiveSearch:IndexName"] ?? "stylenest-products";
        var synonymMapName = configuration["AzureCognitiveSearch:SynonymMapName"] ?? "stylenest-synonyms";

        // Build fields from the ProductSearchDocument model
        var fieldBuilder = new FieldBuilder();
        var fields       = fieldBuilder.Build(typeof(ProductSearchDocument));

        // Attach synonym map to searchable text fields
        foreach (var field in fields.OfType<SearchableField>())
        {
            if (field.Name is "name" or "description")
                field.SynonymMapNames.Add(synonymMapName);
        }

        // Suggester for autocomplete (must reference at least one searchable field)
        var suggester = new SearchSuggester("sg-products", new[] { "name" });

        // Scoring profile: boost Name matches over Description matches
        var scoringProfile = new ScoringProfile("fashion-boost")
        {
            TextWeights = new TextWeights(new Dictionary<string, double>
            {
                { "name",        5.0 },
                { "description", 1.0 },
                { "brand",       3.0 },
                { "category",    2.0 },
            }),
        };

        var index = new SearchIndex(indexName, fields)
        {
            Suggesters     = { suggester },
            ScoringProfiles = { scoringProfile },
            DefaultScoringProfile = "fashion-boost",
        };

        await indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: ct);

        logger.LogInformation(
            "ENH-CAT-006: Search index '{Name}' created/updated with {FieldCount} fields.",
            indexName, fields.Count);
    }
}
