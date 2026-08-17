namespace StyleNest.Auth.API.Services;

public sealed class LoggingOtpDeliveryChannel(ILogger<LoggingOtpDeliveryChannel> logger) : IOtpDeliveryChannel
{
    public Task DeliverAsync(string email, string code, string purpose, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[OTP-DEV] {Purpose} code for {Email}: {Code} (expires 15 min)", purpose, email, code);
        return Task.CompletedTask;
    }
}
