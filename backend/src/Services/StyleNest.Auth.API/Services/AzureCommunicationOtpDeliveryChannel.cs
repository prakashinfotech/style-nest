using Azure.Communication.Email;
using StyleNest.Auth.API.Options;

namespace StyleNest.Auth.API.Services;

public sealed class AzureCommunicationOtpDeliveryChannel(
    EmailClient emailClient,
    OtpDeliveryOptions options,
    ILogger<AzureCommunicationOtpDeliveryChannel> logger) : IOtpDeliveryChannel
{
    public async Task DeliverAsync(string email, string code, string purpose, CancellationToken cancellationToken = default)
    {
        var senderAddress = options.AzureCommunication?.SenderAddress ?? "donotreply@stylenest.com";
        var purposeLabel = purpose switch
        {
            "PasswordReset"     => "Password Reset",
            "EmailVerification" => "Email Verification",
            "PhoneVerification" => "Phone Verification",
            _                   => purpose
        };

        var subject = $"Your StyleNest {purposeLabel} Code";
        var content = new EmailContent(subject)
        {
            Html      = BuildHtml(code, purposeLabel),
            PlainText = $"Your StyleNest {purposeLabel} OTP is: {code}. Valid for 15 minutes. Do not share this code."
        };

        try
        {
            var operation = await emailClient.SendAsync(
                Azure.WaitUntil.Started,
                new EmailMessage(senderAddress, email, content),
                cancellationToken);

            logger.LogInformation("OTP email sent to {Email} for {Purpose} (operationId: {Id})",
                email, purpose, operation.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to {Email} for {Purpose}", email, purpose);
            throw;
        }
    }

    private static string BuildHtml(string code, string purposeLabel) => $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#F5F5F5;font-family:Arial,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr><td align="center" style="padding:40px 20px">
              <table width="600" cellpadding="0" cellspacing="0"
                     style="background:#FFFFFF;border-radius:8px;overflow:hidden;max-width:600px">
                <tr><td style="background:#1C2B4A;padding:24px 32px">
                  <h1 style="margin:0;color:#FFFFFF;font-size:22px;font-weight:700">StyleNest</h1>
                </td></tr>
                <tr><td style="padding:40px 32px">
                  <h2 style="margin:0 0 8px;color:#1A1A1A;font-size:20px">{{purposeLabel}} Code</h2>
                  <p style="margin:0 0 32px;color:#757575;font-size:14px">
                    Use the code below to complete your {{purposeLabel.ToLower()}}.
                    It expires in <strong>15 minutes</strong>.
                  </p>
                  <div style="background:#F5F5F5;border-radius:8px;padding:24px;text-align:center;margin-bottom:32px">
                    <span style="font-size:36px;font-weight:700;letter-spacing:12px;color:#1C2B4A">{{code}}</span>
                  </div>
                  <p style="margin:0;color:#757575;font-size:12px">
                    If you did not request this, please ignore this email.
                    Never share this code with anyone.
                  </p>
                </td></tr>
                <tr><td style="background:#F5F5F5;padding:16px 32px;text-align:center">
                  <p style="margin:0;color:#9E9E9E;font-size:11px">&copy; 2026 StyleNest Fashion. All rights reserved.</p>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
