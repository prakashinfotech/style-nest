using Microsoft.AspNetCore.Mvc;
using StyleNest.Auth.API.DTOs;
using StyleNest.Auth.API.Services;

namespace StyleNest.Auth.API.Controllers;

/// <summary>
/// ENH-AUTH-012 — MFA completion for Admin / SuperAdmin.
/// POST /api/v1/auth/mfa/verify — verifies OTP and issues JWT with mfa_verified=true.
/// </summary>
[ApiController]
[Route("api/v1/auth/mfa")]
public sealed class MfaController(IMfaService mfaService) : ControllerBase
{
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] MfaVerifyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.MfaToken))
            return BadRequest(new { message = "MfaToken is required." });
        if (string.IsNullOrWhiteSpace(request.OtpCode))
            return BadRequest(new { message = "OtpCode is required." });

        var result = await mfaService.CompleteAsync(request.MfaToken, request.OtpCode, ct);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "Auth.MfaInvalidCode" ? 401 : 400;
            return StatusCode(status, new { result.Error.Code, result.Error.Message });
        }

        return Ok(result.Value);
    }
}
