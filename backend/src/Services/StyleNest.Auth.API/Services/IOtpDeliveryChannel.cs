namespace StyleNest.Auth.API.Services;

public interface IOtpDeliveryChannel
{
    Task DeliverAsync(string email, string code, string purpose, CancellationToken cancellationToken = default);
}
