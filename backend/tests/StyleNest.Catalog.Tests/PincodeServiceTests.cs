/**
 * ENH-PDP-001 — Pincode Delivery Estimate
 * Acceptance criteria tested here:
 *   - Returns serviceability, COD eligibility, ETA, express availability, free-delivery threshold
 *   - Non-serviceable pincode → { serviceable: false, codEligible: false, etaDays: 0, expressAvailable: false }
 *   - COD blacklisted pincode → { codEligible: false }
 *   - Unknown pincode → degraded defaults (serviceable: true, etaDays: 5)
 *   - 12 seeded pincode types covered
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class PincodeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PincodeService _sut;

    public PincodeServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new PincodeService(_db, NullLogger<PincodeService>.Instance);
        SeedPincodes();
    }

    public void Dispose() => _db.Dispose();

    // ── seed helper ───────────────────────────────────────────────────────────

    private void SeedPincodes()
    {
        var now = DateTime.UtcNow;
        _db.PincodeServiceabilities.AddRange(
            // 1. Mumbai — serviceable, COD eligible, express
            Pincode("400001", serviceable: true,  cod: true,  eta: 1, express: true,  city: "Mumbai"),
            // 2. Mumbai — serviceable, COD eligible, standard
            Pincode("400002", serviceable: true,  cod: true,  eta: 3, express: false, city: "Mumbai"),
            // 3. Mumbai — serviceable, COD blacklisted, express
            Pincode("400003", serviceable: true,  cod: false, eta: 1, express: true,  city: "Mumbai"),
            // 4. Mumbai — serviceable, COD blacklisted, standard
            Pincode("400004", serviceable: true,  cod: false, eta: 3, express: false, city: "Mumbai"),
            // 5. Delhi — non-serviceable
            Pincode("110001", serviceable: false, cod: false, eta: 0, express: false, city: "Delhi"),
            // 6. Delhi — non-serviceable
            Pincode("110002", serviceable: false, cod: false, eta: 0, express: false, city: "Delhi"),
            // 7. Bangalore — serviceable, COD eligible, express
            Pincode("560001", serviceable: true,  cod: true,  eta: 2, express: true,  city: "Bangalore"),
            // 8. Bangalore — serviceable, COD eligible, standard
            Pincode("560002", serviceable: true,  cod: true,  eta: 5, express: false, city: "Bangalore"),
            // 9. Kolkata — serviceable, COD blacklisted, standard
            Pincode("700001", serviceable: true,  cod: false, eta: 4, express: false, city: "Kolkata"),
            // 10. Hyderabad — serviceable, COD eligible, express
            Pincode("500001", serviceable: true,  cod: true,  eta: 1, express: true,  city: "Hyderabad"),
            // 11. Chennai — serviceable, COD eligible, standard
            Pincode("600001", serviceable: true,  cod: true,  eta: 3, express: false, city: "Chennai"),
            // 12. Jaipur — non-serviceable
            Pincode("302001", serviceable: false, cod: false, eta: 0, express: false, city: "Jaipur"));

        _db.SaveChanges();
    }

    private static PincodeServiceability Pincode(
        string code, bool serviceable, bool cod, int eta, bool express,
        string? city = null, decimal threshold = 499m) =>
        new()
        {
            Id                   = Guid.NewGuid(),
            Pincode              = code,
            IsServiceable        = serviceable,
            CodEligible          = cod,
            EtaDays              = eta,
            ExpressAvailable     = express,
            FreeDeliveryThreshold = threshold,
            City                 = city,
        };

    // ── serviceable pincodes ──────────────────────────────────────────────────

    [Fact]
    public async Task GetEstimate_ServiceableCodExpress_ReturnsCorrectFlags()
    {
        var result = await _sut.GetEstimateAsync("400001");

        result.Serviceable.Should().BeTrue();
        result.CodEligible.Should().BeTrue();
        result.ExpressAvailable.Should().BeTrue();
        result.EtaDays.Should().Be(1);
        result.City.Should().Be("Mumbai");
    }

    [Fact]
    public async Task GetEstimate_ServiceableCodStandard_ReturnsCodEligibleNoExpress()
    {
        var result = await _sut.GetEstimateAsync("400002");

        result.Serviceable.Should().BeTrue();
        result.CodEligible.Should().BeTrue();
        result.ExpressAvailable.Should().BeFalse();
        result.EtaDays.Should().Be(3);
    }

    // ── COD-blacklisted pincodes ──────────────────────────────────────────────

    [Fact]
    public async Task GetEstimate_CodBlacklisted_ReturnsCodFalse()
    {
        var result = await _sut.GetEstimateAsync("400003");

        result.Serviceable.Should().BeTrue();
        result.CodEligible.Should().BeFalse("COD is blacklisted for this pincode");
        result.ExpressAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetEstimate_CodBlacklistedStandard_ReturnsBothFalse()
    {
        var result = await _sut.GetEstimateAsync("400004");

        result.Serviceable.Should().BeTrue();
        result.CodEligible.Should().BeFalse();
        result.ExpressAvailable.Should().BeFalse();
        result.EtaDays.Should().Be(3);
    }

    // ── non-serviceable pincodes ──────────────────────────────────────────────

    [Theory]
    [InlineData("110001")]
    [InlineData("110002")]
    [InlineData("302001")]
    public async Task GetEstimate_NonServiceable_ReturnsServiceableFalse(string pincode)
    {
        var result = await _sut.GetEstimateAsync(pincode);

        result.Serviceable.Should().BeFalse();
        result.CodEligible.Should().BeFalse("COD unavailable for non-serviceable area");
        result.ExpressAvailable.Should().BeFalse();
        result.EtaDays.Should().Be(0);
    }

    // ── additional seeded pincodes ────────────────────────────────────────────

    [Fact]
    public async Task GetEstimate_Bangalore_ExpressAvailable()
        => (await _sut.GetEstimateAsync("560001")).ExpressAvailable.Should().BeTrue();

    [Fact]
    public async Task GetEstimate_Bangalore_StandardOnly()
        => (await _sut.GetEstimateAsync("560002")).ExpressAvailable.Should().BeFalse();

    [Fact]
    public async Task GetEstimate_Kolkata_CodBlacklisted()
        => (await _sut.GetEstimateAsync("700001")).CodEligible.Should().BeFalse();

    [Fact]
    public async Task GetEstimate_Hyderabad_ExpressAndCod()
    {
        var r = await _sut.GetEstimateAsync("500001");
        r.ExpressAvailable.Should().BeTrue();
        r.CodEligible.Should().BeTrue();
        r.EtaDays.Should().Be(1);
    }

    [Fact]
    public async Task GetEstimate_Chennai_StandardCod()
    {
        var r = await _sut.GetEstimateAsync("600001");
        r.Serviceable.Should().BeTrue();
        r.CodEligible.Should().BeTrue();
        r.ExpressAvailable.Should().BeFalse();
        r.EtaDays.Should().Be(3);
    }

    // ── free delivery threshold ───────────────────────────────────────────────

    [Fact]
    public async Task GetEstimate_ReturnsDefaultFreeDeliveryThreshold()
        => (await _sut.GetEstimateAsync("400001")).FreeDeliveryThreshold.Should().Be(499m);

    // ── unknown / degraded ────────────────────────────────────────────────────

    [Fact]
    public async Task GetEstimate_UnknownPincode_ReturnsDegradedDefaults()
    {
        var result = await _sut.GetEstimateAsync("999999");

        result.Serviceable.Should().BeTrue("unknown pincodes assume serviceable");
        result.EtaDays.Should().Be(5);
    }

    [Fact]
    public async Task GetEstimate_EmptyPincode_ReturnsDegradedDefaults()
    {
        var result = await _sut.GetEstimateAsync(string.Empty);

        result.Serviceable.Should().BeTrue("degraded default assumes serviceable");
        result.EtaDays.Should().Be(5);
        result.CodEligible.Should().BeTrue();
        result.ExpressAvailable.Should().BeFalse();
    }
}
