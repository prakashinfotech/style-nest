using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Order.API.Services;

namespace StyleNest.Order.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[AllowAnonymous]
public sealed class PaymentsWebhookController(
    IPaymentWebhookService webhookService,
    ILogger<PaymentsWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Receives Razorpay payment webhook events.
    /// Signature is verified via HMAC-SHA256 constant-time comparison before any processing.
    /// Mismatch returns HTTP 401 and emits a structured audit log (ENH-PAY-002 / FR-PAY-009).
    /// </summary>
    [HttpPost("webhook")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RazorpayWebhook(CancellationToken ct)
    {
        Request.EnableBuffering();

        using var reader  = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var       rawBody = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault() ?? string.Empty;

        if (!webhookService.VerifySignature(rawBody, signature))
        {
            logger.LogWarning(
                "Webhook signature mismatch — RemoteIp:{RemoteIp} SignaturePrefix:{Prefix}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                signature.Length > 8 ? string.Concat(signature.AsSpan(0, 8), "…") : "(empty)");

            return Unauthorized(new { errorCode = "WEBHOOK_SIGNATURE_INVALID" });
        }

        await webhookService.ProcessAsync(rawBody, ct);
        return Ok();
    }
}
