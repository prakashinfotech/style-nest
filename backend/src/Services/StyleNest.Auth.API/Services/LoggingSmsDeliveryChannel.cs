namespace StyleNest.Auth.API.Services;

public sealed class LoggingSmsDeliveryChannel(ILogger<LoggingSmsDeliveryChannel> logger) : ISmsDeliveryChannel
{
    public Task DeliverAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SMS-DEV] OTP for {Phone}: {Code} (expires 300s)", phoneNumber, code);
        return Task.CompletedTask;
    }
}
