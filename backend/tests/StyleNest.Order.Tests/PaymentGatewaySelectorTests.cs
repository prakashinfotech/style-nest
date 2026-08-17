/**
 * ENH-PAY-001 — PayU Payment Gateway Failover (SOW §2.1 / FR-PAY)
 * Acceptance criteria:
 *   - SelectAsync returns Primary when primary health check passes
 *   - SelectAsync returns Fallback when primary health check fails
 *   - SelectAsync returns Fallback when both health checks fail (defensive)
 *   - SelectAsync logs PAYMENT_GATEWAY_FAILOVER when switching to fallback
 *   - HttpGatewayHealthCheck: no URL configured → returns true (optimistic)
 *   - PaymentGatewaySettings.Primary defaults to "RAZORPAY"
 *   - PaymentGatewaySettings.Fallback defaults to "PAYU"
 *   - PaymentGatewaySettings.HealthCheckTimeoutSeconds defaults to 3
 */

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class PaymentGatewaySelectorTests
{
    // ── default settings values ────────────────────────────────────────────────

    [Fact]
    public void PaymentGatewaySettings_DefaultPrimary_IsRazorpay()
        => new PaymentGatewaySettings().Primary.Should().Be("RAZORPAY");

    [Fact]
    public void PaymentGatewaySettings_DefaultFallback_IsPayU()
        => new PaymentGatewaySettings().Fallback.Should().Be("PAYU");

    [Fact]
    public void PaymentGatewaySettings_DefaultHealthCheckTimeout_IsThreeSeconds()
        => new PaymentGatewaySettings().HealthCheckTimeoutSeconds.Should().Be(3);

    // ── SelectAsync — primary healthy ──────────────────────────────────────────

    [Fact]
    public async Task SelectAsync_PrimaryHealthy_ReturnsPrimary()
    {
        var (sut, _) = BuildSut(primaryHealthy: true);

        var result = await sut.SelectAsync();

        result.Should().Be("RAZORPAY");
    }

    // ── SelectAsync — primary unhealthy → fallback ─────────────────────────────

    [Fact]
    public async Task SelectAsync_PrimaryUnhealthy_ReturnsFallback()
    {
        var (sut, _) = BuildSut(primaryHealthy: false);

        var result = await sut.SelectAsync();

        result.Should().Be("PAYU");
    }

    [Fact]
    public async Task SelectAsync_BothUnhealthy_ReturnsFallback()
    {
        // Even when both are down the selector must return fallback
        // (user can still attempt checkout; gateway handles the error)
        var (sut, _) = BuildSut(primaryHealthy: false, fallbackHealthy: false);

        var result = await sut.SelectAsync();

        result.Should().Be("PAYU");
    }

    // ── SelectAsync — logging ──────────────────────────────────────────────────

    [Fact]
    public async Task SelectAsync_PrimaryUnhealthy_LogsFailoverEvent()
    {
        var logger = new FakeLogger<PaymentGatewaySelector>();
        var (sut, _) = BuildSut(primaryHealthy: false, logger: logger);

        await sut.SelectAsync();

        logger.LastMessage.Should().Contain("PAYMENT_GATEWAY_FAILOVER");
        logger.LastMessage.Should().Contain("RAZORPAY");
        logger.LastMessage.Should().Contain("PAYU");
    }

    [Fact]
    public async Task SelectAsync_PrimaryHealthy_DoesNotLogFailover()
    {
        var logger = new FakeLogger<PaymentGatewaySelector>();
        var (sut, _) = BuildSut(primaryHealthy: true, logger: logger);

        await sut.SelectAsync();

        logger.LastMessage.Should().BeNull();
    }

    // ── health check: no URL configured → optimistic ───────────────────────────

    [Fact]
    public async Task HttpGatewayHealthCheck_NoUrlConfigured_ReturnsTrue()
    {
        var settings = Options.Create(new PaymentGatewaySettings
        {
            HealthCheckUrls = []   // empty → no URL for "RAZORPAY"
        });
        var httpFactory = new Mock<IHttpClientFactory>();
        var sut = new HttpGatewayHealthCheck(
            httpFactory.Object, settings, NullLogger<HttpGatewayHealthCheck>.Instance);

        var result = await sut.IsAvailableAsync("RAZORPAY");

        result.Should().BeTrue();
        // HttpClientFactory was never called
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    // ── configurable gateway names ─────────────────────────────────────────────

    [Fact]
    public async Task SelectAsync_CustomGatewayNames_WorksCorrectly()
    {
        var settings = Options.Create(new PaymentGatewaySettings
        {
            Primary  = "STRIPE",
            Fallback = "BRAINTREE",
        });
        var healthMock = new Mock<IGatewayHealthCheck>();
        healthMock.Setup(h => h.IsAvailableAsync("STRIPE",     It.IsAny<CancellationToken>())).ReturnsAsync(false);
        healthMock.Setup(h => h.IsAvailableAsync("BRAINTREE",  It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = new PaymentGatewaySelector(
            healthMock.Object, settings, NullLogger<PaymentGatewaySelector>.Instance);

        var result = await sut.SelectAsync();

        result.Should().Be("BRAINTREE");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static (PaymentGatewaySelector sut, Mock<IGatewayHealthCheck> healthMock) BuildSut(
        bool primaryHealthy  = true,
        bool fallbackHealthy = true,
        FakeLogger<PaymentGatewaySelector>? logger = null)
    {
        var settings = Options.Create(new PaymentGatewaySettings
        {
            Primary  = "RAZORPAY",
            Fallback = "PAYU",
        });

        var healthMock = new Mock<IGatewayHealthCheck>();
        healthMock
            .Setup(h => h.IsAvailableAsync("RAZORPAY", It.IsAny<CancellationToken>()))
            .ReturnsAsync(primaryHealthy);
        healthMock
            .Setup(h => h.IsAvailableAsync("PAYU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackHealthy);

        var sut = new PaymentGatewaySelector(
            healthMock.Object, settings,
            (logger as ILogger<PaymentGatewaySelector>) ?? NullLogger<PaymentGatewaySelector>.Instance);

        return (sut, healthMock);
    }
}

/// <summary>Minimal ILogger that captures the last warning message for assertions.</summary>
internal sealed class FakeLogger<T> : ILogger<T>
{
    public string? LastMessage { get; private set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel >= LogLevel.Warning)
            LastMessage = formatter(state, exception);
    }
}
