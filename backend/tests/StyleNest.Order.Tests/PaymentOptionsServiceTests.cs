/**
 * ENH-CHKOUT-003 — COD Ceiling Enforcement (TC-PAY-BVA-002 / SOW §3.8)
 * Boundary: cartTotal ≤ ₹50,000 → COD offered; > ₹50,000 → COD absent.
 */

using FluentAssertions;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class PaymentOptionsServiceTests
{
    private readonly PaymentOptionsService _sut = new();

    // ── GetCodEligibility boundary ────────────────────────────────────────────

    [Theory]
    [InlineData(50_000,  true)]   // exactly at ceiling → eligible
    [InlineData(50_001,  false)]  // one rupee over → not eligible
    [InlineData(0,       true)]   // zero cart → eligible
    [InlineData(49_999,  true)]   // well below → eligible
    [InlineData(100_000, false)]  // well above → not eligible
    public void GetCodEligibility_BoundaryValues(decimal cartTotal, bool expected)
    {
        _sut.GetCodEligibility(cartTotal).Should().Be(expected);
    }

    // ── CodCeiling constant ───────────────────────────────────────────────────

    [Fact]
    public void CodCeiling_IsExactly50000()
    {
        PaymentOptionsService.CodCeiling.Should().Be(50_000m);
    }

    // ── GetOptions: COD present at boundary ──────────────────────────────────

    [Fact]
    public void GetOptions_CartTotal50000_IncludesCod()
    {
        var options = _sut.GetOptions(50_000m);

        options.Should().Contain(o => o.Method == "COD");
    }

    // ── GetOptions: COD absent above ceiling ─────────────────────────────────

    [Fact]
    public void GetOptions_CartTotal50001_ExcludesCod()
    {
        var options = _sut.GetOptions(50_001m);

        options.Should().NotContain(o => o.Method == "COD");
    }

    // ── GetOptions: standard methods always present ───────────────────────────

    [Theory]
    [InlineData(1_000)]
    [InlineData(50_001)]
    public void GetOptions_StandardPaymentMethodsAlwaysPresent(decimal cartTotal)
    {
        var options = _sut.GetOptions(cartTotal);

        options.Should().Contain(o => o.Method == "RAZORPAY");
        options.Should().Contain(o => o.Method == "UPI");
        options.Should().Contain(o => o.Method == "NET_BANKING");
        options.Should().Contain(o => o.Method == "WALLET");
    }

    // ── COD option has Eligible = true when present ───────────────────────────

    [Fact]
    public void GetOptions_CodWhenPresent_HasEligibleTrue()
    {
        var options = _sut.GetOptions(10_000m);
        var cod = options.Single(o => o.Method == "COD");

        cod.Eligible.Should().BeTrue();
    }
}
