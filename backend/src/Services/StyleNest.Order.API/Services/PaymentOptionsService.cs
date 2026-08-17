namespace StyleNest.Order.API.Services;

/// <summary>
/// ENH-CHKOUT-003 — COD ceiling enforcement (TC-PAY-BVA-002 / SOW §3.8).
/// COD is offered only when cart total ≤ ₹50,000 (not strictly less than).
/// </summary>
public sealed record PaymentOption(string Method, bool Eligible);

public interface IPaymentOptionsService
{
    bool GetCodEligibility(decimal cartTotal);
    IReadOnlyList<PaymentOption> GetOptions(decimal cartTotal);
}

public sealed class PaymentOptionsService : IPaymentOptionsService
{
    // ENH-CHKOUT-003: BR — COD ceiling at exactly ₹50,000 (TC-PAY-BVA-002)
    public const decimal CodCeiling = 50_000m;

    public bool GetCodEligibility(decimal cartTotal) => cartTotal <= CodCeiling;

    public IReadOnlyList<PaymentOption> GetOptions(decimal cartTotal)
    {
        var options = new List<PaymentOption>
        {
            new("RAZORPAY",    true),
            new("UPI",         true),
            new("NET_BANKING", true),
            new("WALLET",      true),
        };

        if (GetCodEligibility(cartTotal))
            options.Add(new("COD", true));

        return options;
    }
}
