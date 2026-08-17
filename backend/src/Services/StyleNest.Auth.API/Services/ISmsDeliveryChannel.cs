namespace StyleNest.Auth.API.Services;

public interface ISmsDeliveryChannel
{
    Task DeliverAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
