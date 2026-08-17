using Hangfire;

namespace StyleNest.Auth.API.Services;

/// <summary>
/// ENH-NOTIF-004 — IOtpDeliveryChannel that enqueues OtpEmailJob via Hangfire.
/// OTP code is never logged by this channel; only email + purpose are recorded.
/// </summary>
public sealed class HangfireOtpDeliveryChannel(
    IBackgroundJobClient jobClient,
    ILogger<HangfireOtpDeliveryChannel> logger) : IOtpDeliveryChannel
{
    public Task DeliverAsync(string email, string code, string purpose, CancellationToken cancellationToken = default)
    {
        jobClient.Enqueue<OtpEmailJob>(job =>
            job.ExecuteAsync(email, code, purpose, CancellationToken.None));

        // OTP code deliberately NOT logged — production log safety
        logger.LogInformation("OTP delivery job enqueued for {Email} ({Purpose})", email, purpose);
        return Task.CompletedTask;
    }
}
