using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Auth.API.DTOs;
using StyleNest.Auth.API.Services;

namespace StyleNest.Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(dto, ct);
        if (result.IsFailure)
            return BadRequest(new { result.Error.Code, result.Error.Message });
        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        if (result.IsFailure)
        {
            // ENH-AUTH-005: lockout error encodes seconds in Message (Error is sealed)
            // ENH-AUTH-012: MFA required for Admin/SuperAdmin — return 200 with challenge
            if (result.Error.Code == "Auth.MfaRequired")
                return Ok(new { action = "MFA_REQUIRED", mfaToken = result.Error.Message });

            // ENH-AUTH-005: account locked
            if (result.Error.Code == AuthErrors.AccountLockedCode
                && int.TryParse(result.Error.Message, out var seconds))
            {
                return StatusCode(423, new
                {
                    Code    = AuthErrors.AccountLockedCode,
                    Message = $"Account locked after too many failed attempts. Try again in {seconds} seconds.",
                    lockoutDurationSeconds = seconds,
                });
            }
            return Unauthorized(new { result.Error.Code, result.Error.Message });
        }
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.RefreshAsync(dto.RefreshToken, ct);
        if (result.IsFailure)
            return Unauthorized(new { result.Error.Code, result.Error.Message });
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto dto, CancellationToken ct)
    {
        await _authService.LogoutAsync(dto.RefreshToken, ct);
        return NoContent();
    }
}
