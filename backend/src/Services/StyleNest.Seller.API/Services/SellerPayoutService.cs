using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.DTOs;

namespace StyleNest.Seller.API.Services;

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>
/// ENH-SELL-003 — Razorpay Payout API settings.
/// Bind from appsettings.json section <c>"RazorpayPayout"</c>.
/// </summary>
public sealed class RazorpayPayoutSettings
{
    public const string Section = "RazorpayPayout";

    /// <summary>Razorpay X-API key ID (starts with rzp_live_ or rzp_test_).</summary>
    public string KeyId { get; init; } = string.Empty;

    /// <summary>Razorpay X-API key secret.</summary>
    public string KeySecret { get; init; } = string.Empty;

    /// <summary>Razorpay active current account number linked to the payout API.</summary>
    public string AccountNumber { get; init; } = string.Empty;

    /// <summary>Payout currency — default INR.</summary>
    public string Currency { get; init; } = "INR";

    /// <summary>Transfer mode: IMPS | NEFT | RTGS | UPI.</summary>
    public string Mode { get; init; } = "IMPS";
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record TriggerPayoutRequest(
    decimal Amount,
    string? Notes);

public record PayoutResultDto(
    Guid    PayoutId,
    string  Status,
    string? TransactionReference,
    decimal Amount,
    DateTime CreatedAt);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface ISellerPayoutService
{
    /// <summary>
    /// Initiates a Razorpay payout for the given seller.
    /// Creates a <see cref="SellerPayout"/> record in <c>Pending</c> state, calls the
    /// Razorpay Payout API, and updates the record to <c>Processing</c> or <c>Failed</c>.
    /// </summary>
    Task<PayoutResultDto> TriggerPayoutAsync(
        Guid    sellerId,
        decimal amount,
        string? notes           = null,
        CancellationToken ct    = default);

    /// <summary>Returns all payout records for the given seller, newest first.</summary>
    Task<IReadOnlyList<PayoutResultDto>> GetPayoutsAsync(
        Guid              sellerId,
        CancellationToken ct = default);
}

// ── Razorpay Payout API client ────────────────────────────────────────────────

/// <summary>
/// ENH-SELL-003 — Low-level Razorpay X-API payout client.
///
/// Flow:
///   1. POST /v1/fund_accounts — register seller's bank account
///   2. POST /v1/payouts       — initiate payout against the fund account
///
/// Razorpay payout amounts are in <b>paise</b> (1 INR = 100 paise).
/// </summary>
public sealed class RazorpayPayoutClient(
    IHttpClientFactory        httpFactory,
    RazorpayPayoutSettings    settings,
    ILogger<RazorpayPayoutClient> logger)
{
    private const string BaseUrl      = "https://api.razorpay.com/v1";
    private const string HttpClientName = "razorpay-payout";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Registers the seller's bank account as a Razorpay Fund Account.
    /// Returns the <c>fund_account_id</c> on success.
    /// </summary>
    public async Task<string?> CreateFundAccountAsync(
        Guid   sellerId,
        string accountNumber,
        string ifsc,
        string accountHolderName,
        CancellationToken ct = default)
    {
        var client = CreateClient();
        var body   = new
        {
            contact_id  = (string?)null, // omit — use account number only
            account_type = "bank_account",
            bank_account = new
            {
                name           = accountHolderName,
                ifsc,
                account_number = accountNumber,
            }
        };

        try
        {
            var resp = await client.PostAsJsonAsync($"{BaseUrl}/fund_accounts", body, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "ENH-SELL-003: CreateFundAccount failed for seller {SellerId}: {Status} — {Error}",
                    sellerId, (int)resp.StatusCode, err);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return doc.RootElement.GetProperty("id").GetString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ENH-SELL-003: Exception creating fund account for seller {SellerId}", sellerId);
            return null;
        }
    }

    /// <summary>
    /// Initiates a payout to the given <paramref name="fundAccountId"/>.
    /// Returns the Razorpay payout ID on success, <c>null</c> on failure.
    /// </summary>
    public async Task<(string? PayoutId, string? Status)> CreatePayoutAsync(
        string fundAccountId,
        decimal amountInr,
        string  currency,
        string  mode,
        string  narration,
        CancellationToken ct = default)
    {
        var client      = CreateClient();
        var amountPaise = (long)(amountInr * 100); // INR → paise

        var body = new
        {
            account_number = settings.AccountNumber,
            fund_account_id = fundAccountId,
            amount         = amountPaise,
            currency,
            mode,
            purpose        = "payout",
            queue_if_low_balance = true,
            narration,
            reference_id   = Guid.NewGuid().ToString("N")[..20],
        };

        try
        {
            var resp = await client.PostAsJsonAsync($"{BaseUrl}/payouts", body, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "ENH-SELL-003: CreatePayout failed: {Status} — {Error}",
                    (int)resp.StatusCode, err);
                return (null, "failed");
            }

            using var doc    = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var payoutId     = doc.RootElement.GetProperty("id").GetString();
            var status       = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : "processing";

            return (payoutId, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ENH-SELL-003: Exception creating payout");
            return (null, "failed");
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private System.Net.Http.HttpClient CreateClient()
    {
        var client    = httpFactory.CreateClient(HttpClientName);
        var creds     = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.KeyId}:{settings.KeySecret}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", creds);
        return client;
    }
}

// ── Domain service ────────────────────────────────────────────────────────────

/// <summary>
/// ENH-SELL-003 — Seller payout domain service.
///
/// When Razorpay is configured: creates a fund account for the seller's bank
/// details and initiates a Razorpay IMPS/NEFT payout.
/// When not configured (KeyId empty): records the payout in <c>Pending</c> state
/// for manual processing — no-op on the Razorpay side.
/// </summary>
public sealed class SellerPayoutService(
    AppDbContext           db,
    RazorpayPayoutClient   razorpayClient,
    RazorpayPayoutSettings settings,
    ILogger<SellerPayoutService> logger) : ISellerPayoutService
{
    public async Task<PayoutResultDto> TriggerPayoutAsync(
        Guid    sellerId,
        decimal amount,
        string? notes        = null,
        CancellationToken ct = default)
    {
        var seller = await db.Sellers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sellerId, ct)
            ?? throw new KeyNotFoundException($"Seller {sellerId} not found.");

        if (seller.Status != SellerStatus.Active)
            throw new InvalidOperationException(
                $"Cannot trigger payout for seller with status '{seller.Status}'.");

        if (string.IsNullOrWhiteSpace(seller.BankAccountNumber) ||
            string.IsNullOrWhiteSpace(seller.BankIfsc))
            throw new InvalidOperationException(
                "Seller bank account details are incomplete. Update BankAccountNumber and BankIfsc first.");

        // ── Create DB record in Pending state ─────────────────────────────
        var payout = new SellerPayout
        {
            SellerId = sellerId,
            Amount   = amount,
            Status   = PayoutStatus.Pending,
            Notes    = notes,
        };
        db.SellerPayouts.Add(payout);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "ENH-SELL-003: Payout {PayoutId} created for seller {SellerId} — ₹{Amount}",
            payout.Id, sellerId, amount);

        // ── Skip Razorpay if not configured ──────────────────────────────
        if (!IsRazorpayConfigured())
        {
            logger.LogInformation(
                "ENH-SELL-003: Razorpay not configured — payout {PayoutId} stays Pending for manual processing.",
                payout.Id);
            return MapPayout(payout);
        }

        // ── Step 1: Register fund account ─────────────────────────────────
        var accountHolder = seller.StoreName; // use store name as account holder
        var fundAccountId = await razorpayClient.CreateFundAccountAsync(
            sellerId,
            seller.BankAccountNumber!,
            seller.BankIfsc!,
            accountHolder,
            ct);

        if (fundAccountId is null)
        {
            payout.Status  = PayoutStatus.Failed;
            payout.Notes   = $"{notes} | Fund account creation failed";
            await db.SaveChangesAsync(ct);
            return MapPayout(payout);
        }

        // ── Step 2: Initiate payout ───────────────────────────────────────
        var narration = $"Seller payout {payout.Id:N}";
        var (payoutId, status) = await razorpayClient.CreatePayoutAsync(
            fundAccountId, amount, settings.Currency, settings.Mode, narration, ct);

        payout.Status               = payoutId is not null ? PayoutStatus.Processing : PayoutStatus.Failed;
        payout.TransactionReference = payoutId;
        payout.ProcessedAt          = payoutId is not null ? DateTime.UtcNow : null;
        payout.Notes                = $"{notes} | Razorpay status: {status}";

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "ENH-SELL-003: Payout {PayoutId} → Razorpay {RzpPayoutId} status={Status}",
            payout.Id, payoutId, status);

        return MapPayout(payout);
    }

    public async Task<IReadOnlyList<PayoutResultDto>> GetPayoutsAsync(
        Guid              sellerId,
        CancellationToken ct = default)
    {
        var payouts = await db.SellerPayouts
            .AsNoTracking()
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return payouts.Select(MapPayout).ToList();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private bool IsRazorpayConfigured() =>
        !string.IsNullOrWhiteSpace(settings.KeyId) &&
        !settings.KeyId.StartsWith("REPLACE") &&
        !string.IsNullOrWhiteSpace(settings.AccountNumber);

    private static PayoutResultDto MapPayout(SellerPayout p) =>
        new(p.Id, p.Status.ToString(), p.TransactionReference, p.Amount, p.CreatedAt);
}
