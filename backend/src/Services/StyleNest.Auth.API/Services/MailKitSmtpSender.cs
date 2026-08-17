using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using StyleNest.Auth.API.Options;

namespace StyleNest.Auth.API.Services;

public sealed class MailKitSmtpSender(SmtpEmailOptions options) : ISmtpMailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, string plainText, CancellationToken ct = default)
    {
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.SenderAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new MultipartAlternative
        {
            new TextPart(TextFormat.Plain) { Text = plainText },
            new TextPart(TextFormat.Html)  { Text = htmlBody },
        };

        using var client = new SmtpClient();
        var socketOptions = string.IsNullOrEmpty(options.Username)
            ? SecureSocketOptions.None
            : SecureSocketOptions.Auto;
        await client.ConnectAsync(options.Host, options.Port, socketOptions, ct);
        if (!string.IsNullOrEmpty(options.Username))
            await client.AuthenticateAsync(options.Username, options.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
