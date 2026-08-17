using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

/// <summary>ENH-PAY-002 — HMAC-SHA256 webhook signature verification tests.</summary>
public sealed class PaymentWebhookServiceTests : IDisposable
{
    private const string WebhookSecret = "test-webhook-secret-12345";

    private readonly AppDbContext _db;
    private readonly PaymentWebhookService _sut;

    public PaymentWebhookServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RazorpayWebhook:Secret"] = WebhookSecret
            })
            .Build();

        _sut = new PaymentWebhookService(_db, config, NullLogger<PaymentWebhookService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string ComputeHmac(string secret, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        const string body = "{\"event\":\"payment.captured\"}";
        var sig = ComputeHmac(WebhookSecret, body);

        _sut.VerifySignature(body, sig).Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_TamperedBody_ReturnsFalse()
    {
        var sig = ComputeHmac(WebhookSecret, "{\"event\":\"payment.captured\"}");

        _sut.VerifySignature("{\"event\":\"payment.failed\"}", sig).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WrongSecret_ReturnsFalse()
    {
        const string body = "{\"event\":\"payment.captured\"}";
        var sig = ComputeHmac("wrong-secret", body);

        _sut.VerifySignature(body, sig).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_EmptySignature_ReturnsFalse()
    {
        _sut.VerifySignature("{}", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_TruncatedSignature_ReturnsFalse()
    {
        const string body = "{}";
        var truncated = ComputeHmac(WebhookSecret, body)[..10];

        _sut.VerifySignature(body, truncated).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_EmptySecret_ReturnsFalse()
    {
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["RazorpayWebhook:Secret"] = "" })
            .Build();
        var sut = new PaymentWebhookService(_db, emptyConfig, NullLogger<PaymentWebhookService>.Instance);

        const string body = "{}";
        var sig = ComputeHmac(WebhookSecret, body);

        sut.VerifySignature(body, sig).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_UppercaseSignature_IsCaseInsensitive()
    {
        const string body = "{\"event\":\"payment.captured\"}";
        var sig = ComputeHmac(WebhookSecret, body).ToUpperInvariant();

        _sut.VerifySignature(body, sig).Should().BeTrue();
    }
}
