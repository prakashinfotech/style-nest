/**
 * ENH-PDP-002 — EMI Calculator
 * Acceptance criteria:
 *   - Bank-wise tenures (3/6/9/12/24m) with correct instalment amounts
 *   - No-cost EMI options flagged (bank subsidises the interest)
 *   - EMI panel hidden for orders below minOrderForEmi config (default ₹3,000)
 *   - Formula: instalment = principal × (rate/12) / (1 - (1 + rate/12)^-tenure)
 *   - Instalment accurate to ₹1 (rounded up to nearest rupee)
 */

namespace StyleNest.Catalog.API.Services;

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>ENH-PDP-002 — EMI calculator configuration.</summary>
public sealed class EmiSettings
{
    public const string Section = "Emi";

    /// <summary>Minimum order value (₹) for EMI to be offered.</summary>
    public decimal MinOrderForEmi { get; init; } = 3_000m;

    /// <summary>Configured bank EMI plans.</summary>
    public List<BankEmiPlan> Banks { get; init; } = DefaultBanks();

    private static List<BankEmiPlan> DefaultBanks() =>
    [
        new("HDFC Bank",  0.12m, [3,6,9,12,24], NoCostTenures: [3]),
        new("ICICI Bank", 0.14m, [3,6,9,12],    NoCostTenures: [3,6]),
        new("SBI",        0.10m, [3,6,9,12,24], NoCostTenures: []),
        new("Axis Bank",  0.13m, [3,6,12],       NoCostTenures: [3]),
    ];
}

/// <summary>Bank-level EMI plan definition.</summary>
public sealed record BankEmiPlan(
    string       BankName,
    decimal      AnnualRate,       // e.g. 0.12 = 12% p.a.
    List<int>    Tenures,          // months
    List<int>    NoCostTenures);   // tenures where interest is zero (bank subsidised)

// ── Response records ──────────────────────────────────────────────────────────

/// <summary>ENH-PDP-002 — EMI options returned for a given order amount.</summary>
public sealed record EmiOptionsResponse(
    bool               Eligible,
    decimal            OrderAmount,
    decimal            MinOrderForEmi,
    List<BankEmiOption> Banks);

public sealed record BankEmiOption(
    string            BankName,
    List<EmiTenure>   Tenures);

public sealed record EmiTenure(
    int      Months,
    decimal  MonthlyInstalment,
    bool     IsNoCostEmi,
    decimal  TotalInterest);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IEmiCalculatorService
{
    /// <summary>
    /// ENH-PDP-002 — Returns available EMI options for the given order amount.
    /// Returns <see cref="EmiOptionsResponse.Eligible"/> = false when amount is below minimum.
    /// </summary>
    EmiOptionsResponse GetOptions(decimal orderAmount);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// ENH-PDP-002 — Computes bank-wise EMI tenures using standard reducing-balance formula.
/// No-cost EMI: instalment = principal / tenure (no interest charged — bank subsidises).
/// Regular EMI: instalment = P × (r/12) / (1 − (1 + r/12)^−n), rounded up to ₹1.
/// </summary>
public sealed class EmiCalculatorService(
    Microsoft.Extensions.Options.IOptions<EmiSettings> options) : IEmiCalculatorService
{
    public EmiOptionsResponse GetOptions(decimal orderAmount)
    {
        var cfg = options.Value;

        if (orderAmount < cfg.MinOrderForEmi)
            return new EmiOptionsResponse(
                Eligible:       false,
                OrderAmount:    orderAmount,
                MinOrderForEmi: cfg.MinOrderForEmi,
                Banks:          []);

        var banks = cfg.Banks
            .Select(bank => new BankEmiOption(
                BankName: bank.BankName,
                Tenures: bank.Tenures
                    .Select(n =>
                    {
                        bool isNoCost = bank.NoCostTenures.Contains(n);
                        decimal instalment;
                        decimal totalInterest;

                        if (isNoCost)
                        {
                            // No-cost EMI: simple equal split, bank covers interest
                            instalment    = Math.Ceiling(orderAmount / n);
                            totalInterest = 0m;
                        }
                        else
                        {
                            // Standard reducing-balance EMI
                            instalment    = ComputeInstalment(orderAmount, bank.AnnualRate, n);
                            totalInterest = Math.Round((instalment * n) - orderAmount, 2);
                        }

                        return new EmiTenure(n, instalment, isNoCost, totalInterest);
                    })
                    .ToList()))
            .ToList();

        return new EmiOptionsResponse(
            Eligible:       true,
            OrderAmount:    orderAmount,
            MinOrderForEmi: cfg.MinOrderForEmi,
            Banks:          banks);
    }

    // ── formula ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Standard reducing-balance EMI formula.
    /// <c>EMI = P × r / (1 − (1 + r)^-n)</c>  where <c>r = annualRate / 12</c>.
    /// Result is rounded up to the nearest rupee.
    /// </summary>
    public static decimal ComputeInstalment(decimal principal, decimal annualRate, int tenureMonths)
    {
        if (annualRate == 0m) return Math.Ceiling(principal / tenureMonths);

        double r   = (double)(annualRate / 12m);
        double n   = tenureMonths;
        double p   = (double)principal;
        double emi = p * r / (1.0 - Math.Pow(1.0 + r, -n));

        return (decimal)Math.Ceiling(emi);   // round up to ₹1
    }
}
