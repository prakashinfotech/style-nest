using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace StyleNest.Admin.API.Services;

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>
/// ENH-AI-003 — Azure OpenAI Product Description Assistant settings.
/// Bind from appsettings.json section <c>"AzureOpenAI"</c>.
/// </summary>
public sealed class AzureOpenAiSettings
{
    public const string Section = "AzureOpenAI";

    /// <summary>Azure OpenAI endpoint (e.g. "https://my-resource.openai.azure.com/").</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Azure OpenAI API key.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>The deployment name (e.g. "gpt-4o").</summary>
    public string DeploymentName { get; init; } = "gpt-4o";

    /// <summary>Maximum tokens to generate for the description.</summary>
    public int MaxTokens { get; init; } = 500;

    /// <summary>Temperature for generation (0.0 – 1.0). 0.7 balances creativity and precision.</summary>
    public float Temperature { get; init; } = 0.7f;
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record GenerateDescriptionRequest(
    string ProductName,
    string Category,
    string Brand,
    decimal Price,
    string? ExistingDescription = null,
    IReadOnlyList<string>? Keywords = null);

public record GenerateDescriptionResponse(
    string Description,
    string? Disclaimer = "AI-generated — review before publishing.");

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IProductDescriptionAssistant
{
    /// <summary>
    /// Generates a rich product description using Azure OpenAI GPT-4.
    /// Returns the existing description (or a placeholder) when Azure OpenAI is not configured.
    /// </summary>
    Task<GenerateDescriptionResponse> GenerateAsync(
        GenerateDescriptionRequest request,
        CancellationToken          ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// ENH-AI-003 — Azure OpenAI Product Description Assistant.
///
/// Generates SEO-friendly, brand-voice-consistent product descriptions for use
/// in the admin CMS. The system prompt follows StyleNest Fashion's luxury-tier
/// tone: aspirational, concise, feature-driven.
///
/// When the Azure OpenAI endpoint / API key is not configured the service falls
/// back to a structured template description so the admin CMS remains usable
/// without Azure credentials in development.
/// </summary>
public sealed class ProductDescriptionAssistant(
    AzureOpenAiSettings settings,
    ILogger<ProductDescriptionAssistant> logger) : IProductDescriptionAssistant
{
    private const string SystemPrompt =
        """
        You are a luxury fashion copywriter for StyleNest Fashion, India's premium online fashion destination.
        Write concise, aspirational product descriptions (3-4 sentences, max 120 words) that:
        - Open with the hero feature or occasion the item is suited for
        - Highlight material quality, craftsmanship, or unique design details
        - Include 1-2 relevant lifestyle cues (workwear, festive, casual)
        - Close with a subtle call-to-action
        - Use present tense, second person ("You")
        - Never use exclamation marks or superlatives like "best" or "amazing"
        - Output the description ONLY — no quotes, no introduction, no meta-text
        """;

    public async Task<GenerateDescriptionResponse> GenerateAsync(
        GenerateDescriptionRequest request,
        CancellationToken          ct = default)
    {
        if (!IsConfigured())
        {
            logger.LogInformation(
                "ENH-AI-003: Azure OpenAI not configured — returning template description for '{Product}'",
                request.ProductName);
            return new GenerateDescriptionResponse(BuildFallbackDescription(request));
        }

        var userMessage = BuildUserPrompt(request);
        logger.LogInformation(
            "ENH-AI-003: Generating description for '{Product}' ({Category}) via {Deployment}",
            request.ProductName, request.Category, settings.DeploymentName);

        try
        {
            var client = new AzureOpenAIClient(
                new Uri(settings.Endpoint),
                new AzureKeyCredential(settings.ApiKey));

            var chatClient = client.GetChatClient(settings.DeploymentName);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(userMessage),
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = settings.MaxTokens,
                Temperature         = settings.Temperature,
            };

            var completion = await chatClient.CompleteChatAsync(messages, options, ct);
            var text       = completion.Value.Content[0].Text.Trim();

            logger.LogInformation(
                "ENH-AI-003: Description generated for '{Product}' — {TokenCount} tokens used.",
                request.ProductName,
                completion.Value.Usage?.TotalTokenCount ?? 0);

            return new GenerateDescriptionResponse(text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "ENH-AI-003: Azure OpenAI call failed for '{Product}'. Returning fallback.",
                request.ProductName);
            return new GenerateDescriptionResponse(
                BuildFallbackDescription(request),
                "AI generation failed — using template. Review before publishing.");
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(settings.Endpoint) &&
        !settings.Endpoint.StartsWith("REPLACE") &&
        !string.IsNullOrWhiteSpace(settings.ApiKey) &&
        !settings.ApiKey.StartsWith("REPLACE");

    private static string BuildUserPrompt(GenerateDescriptionRequest req)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Product name: {req.ProductName}");
        sb.AppendLine($"Category: {req.Category}");
        sb.AppendLine($"Brand: {req.Brand}");
        sb.AppendLine($"Price: ₹{req.Price:N0}");

        if (!string.IsNullOrWhiteSpace(req.ExistingDescription))
            sb.AppendLine($"Existing description to improve: {req.ExistingDescription}");

        if (req.Keywords?.Count > 0)
            sb.AppendLine($"Keywords to include: {string.Join(", ", req.Keywords)}");

        return sb.ToString();
    }

    private static string BuildFallbackDescription(GenerateDescriptionRequest req)
    {
        var priceRange = req.Price >= 10000 ? "premium" : req.Price >= 3000 ? "mid-range" : "accessible";
        return $"Elevate your wardrobe with the {req.ProductName} by {req.Brand}. " +
               $"This {priceRange} {req.Category.ToLowerInvariant()} piece is crafted for the discerning shopper " +
               $"who values quality and style. Add it to your collection today.";
    }
}
