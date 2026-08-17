namespace StyleNest.Auth.API.Services;

public interface ISmtpMailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string plainText, CancellationToken ct = default);
}
