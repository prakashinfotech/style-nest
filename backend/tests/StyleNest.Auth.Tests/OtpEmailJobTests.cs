/**
 * ENH-NOTIF-004 — OtpEmailJob + HangfireOtpDeliveryChannel unit tests
 * Acceptance criteria:
 *  - Subject contains "verification code"
 *  - Email sent to correct recipient
 *  - OTP code appears in email body (HTML contains the code)
 *  - OTP code never appears in ILogger output (production log safety)
 *  - HangfireOtpDeliveryChannel enqueues a job without logging the OTP
 */

using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StyleNest.Auth.API.Services;
using Xunit;

namespace StyleNest.Auth.Tests;

public sealed class OtpEmailJobTests
{
    private static OtpEmailJob MakeJob(ISmtpMailSender sender, ILogger<OtpEmailJob>? logger = null)
        => new(sender, logger ?? NullLogger<OtpEmailJob>.Instance);

    // ── Subject contains "verification code" ──────────────────────────────────

    [Theory]
    [InlineData("EmailVerification")]
    [InlineData("PasswordReset")]
    [InlineData("PhoneVerification")]
    public async Task ExecuteAsync_SubjectContainsVerificationCode(string purpose)
    {
        var sender = new Mock<ISmtpMailSender>();
        string? capturedSubject = null;
        sender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, string, string, string, CancellationToken>((_, subj, _, _, _) => capturedSubject = subj)
              .Returns(Task.CompletedTask);

        await MakeJob(sender.Object).ExecuteAsync("u@example.com", "123456", purpose);

        capturedSubject.Should().ContainEquivalentOf("verification code");
    }

    // ── Email sent to correct recipient ───────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SendsEmailToCorrectRecipient()
    {
        var sender = new Mock<ISmtpMailSender>();
        string? capturedTo = null;
        sender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, string, string, string, CancellationToken>((to, _, _, _, _) => capturedTo = to)
              .Returns(Task.CompletedTask);

        await MakeJob(sender.Object).ExecuteAsync("recipient@example.com", "654321", "EmailVerification");

        capturedTo.Should().Be("recipient@example.com");
    }

    // ── OTP code appears in HTML body ─────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_HtmlBodyContainsOtpCode()
    {
        var sender = new Mock<ISmtpMailSender>();
        string? capturedHtml = null;
        sender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, string, string, string, CancellationToken>((_, _, html, _, _) => capturedHtml = html)
              .Returns(Task.CompletedTask);

        await MakeJob(sender.Object).ExecuteAsync("u@example.com", "987654", "EmailVerification");

        capturedHtml.Should().Contain("987654");
    }

    // ── OTP code never appears in logger output ───────────────────────────────

    [Fact]
    public async Task ExecuteAsync_DoesNotLogOtpCode()
    {
        var sender = new Mock<ISmtpMailSender>();
        sender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<OtpEmailJob>>();

        await MakeJob(sender.Object, logger.Object).ExecuteAsync("u@example.com", "111222", "PasswordReset");

        // OTP code must not appear in any log message
        logger.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("111222")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ── ISmtpMailSender called exactly once ───────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CallsSenderExactlyOnce()
    {
        var sender = new Mock<ISmtpMailSender>();
        sender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await MakeJob(sender.Object).ExecuteAsync("u@example.com", "333444", "EmailVerification");

        sender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Sender failure propagates ─────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_PropagatesSenderException()
    {
        var sender = new Mock<ISmtpMailSender>();
        sender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));

        var act = () => MakeJob(sender.Object).ExecuteAsync("u@example.com", "555666", "EmailVerification");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("SMTP unreachable");
    }
}

public sealed class HangfireOtpDeliveryChannelTests
{
    // ── Job is enqueued (IBackgroundJobClient.Create called) ──────────────────

    [Fact]
    public async Task DeliverAsync_EnqueuesHangfireJob()
    {
        var jobClient = new Mock<IBackgroundJobClient>();
        jobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");

        var channel = new HangfireOtpDeliveryChannel(jobClient.Object, NullLogger<HangfireOtpDeliveryChannel>.Instance);

        await channel.DeliverAsync("u@example.com", "777888", "EmailVerification");

        jobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    // ── OTP code not logged by channel ────────────────────────────────────────

    [Fact]
    public async Task DeliverAsync_DoesNotLogOtpCode()
    {
        var jobClient = new Mock<IBackgroundJobClient>();
        jobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-2");

        var logger = new Mock<ILogger<HangfireOtpDeliveryChannel>>();
        var channel = new HangfireOtpDeliveryChannel(jobClient.Object, logger.Object);

        await channel.DeliverAsync("u@example.com", "999000", "PasswordReset");

        logger.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("999000")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ── DeliverAsync returns synchronously (does not await job) ──────────────

    [Fact]
    public async Task DeliverAsync_CompletesWithoutAwaitingJob()
    {
        var jobClient = new Mock<IBackgroundJobClient>();
        jobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-3");

        var channel = new HangfireOtpDeliveryChannel(jobClient.Object, NullLogger<HangfireOtpDeliveryChannel>.Instance);

        // Should complete promptly — no long async wait
        var task = channel.DeliverAsync("u@example.com", "112233", "EmailVerification");
        task.IsCompleted.Should().BeTrue();
        await task; // should not throw
    }
}
