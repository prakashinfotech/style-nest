using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Auth.API.Services;

namespace StyleNest.Auth.API.Controllers;

/// <summary>
/// ENH-AUTH-004 — Multi-device session management.
/// GET    /api/v1/auth/sessions           — list all active sessions for current user
/// DELETE /api/v1/auth/sessions/{id}      — revoke a specific session
/// DELETE /api/v1/auth/sessions           — revoke all sessions except the current one
/// </summary>
[ApiController]
[Route("api/v1/auth/sessions")]
[Authorize]
public sealed class SessionsController(ISessionService sessionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSessions(
        [FromHeader(Name = "X-Refresh-Token")] string? currentToken,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var sessions = await sessionService.GetSessionsAsync(userId.Value, currentToken, ct);
        return Ok(sessions);
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var revoked = await sessionService.RevokeSessionAsync(userId.Value, sessionId, ct);
        if (!revoked)
            return NotFound(new { message = "Session not found or already revoked." });

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> RevokeAllSessions(
        [FromHeader(Name = "X-Refresh-Token")] string? currentToken,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await sessionService.RevokeAllSessionsAsync(userId.Value, currentToken, ct);
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
