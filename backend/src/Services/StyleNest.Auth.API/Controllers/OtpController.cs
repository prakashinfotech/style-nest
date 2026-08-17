using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StyleNest.Auth.API.DTOs;
using StyleNest.Auth.API.Services;

namespace StyleNest.Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class OtpController(IOtpService otpService) : ControllerBase
{
    // POST /api/v1/auth/otp/send — phone-based OTP (FR-AUTH-001)
    // ENH-AUTH-010 — 5 requests / IP / 1 h sliding window; excess → 429 + Retry-After: 3600
    [HttpPost("otp/send")]
    [EnableRateLimiting("otp-per-ip")]
    public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpRequest request)
    {
        var result = await otpService.SendPhoneOtpAsync(request.PhoneNumber);
        if (result.IsFailure)
        {
            return result.Error.Code == "OTP.RateLimitExceeded"
                ? StatusCode(429, new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });
        }
        return Ok(new SendPhoneOtpResponse(result.Value.MaskedPhone, result.Value.ExpiresAt));
    }

    // POST /api/v1/auth/forgot-password — email-based password reset
    // ENH-AUTH-010 — same IP-lock policy: prevents email enumeration via reset-OTP flooding
    [HttpPost("forgot-password")]
    [EnableRateLimiting("otp-per-ip")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await otpService.SendForgotPasswordOtpAsync(request.Email);
        return Ok(new { Message = "If that email exists, a reset code has been sent." });
    }

    // POST /api/v1/auth/verify-otp — verify email-based OTP (returns 410 on expired)
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await otpService.VerifyOtpAsync(request.Email, request.Code, request.Purpose);
        if (result.IsFailure)
        {
            return result.Error.Code == "AUTH_OTP_EXPIRED"
                ? StatusCode(410, new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });
        }
        return Ok(new { Message = "OTP verified successfully." });
    }

    // POST /api/v1/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await otpService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
        if (result.IsFailure)
        {
            return result.Error.Code == "AUTH_OTP_EXPIRED"
                ? StatusCode(410, new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });
        }
        return Ok(new { Message = "Password reset successfully." });
    }
}
