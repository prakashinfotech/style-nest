/**
 * ENH-PAY-006 — Razorpay Vault Tokenisation endpoints.
 * All routes require authentication; user identity is extracted from the JWT sub claim.
 * No PAN or CVV is ever accepted or returned by this controller.
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Order.API.Services;

namespace StyleNest.Order.API.Controllers;

/// <summary>ENH-PAY-006 — Saved card token management (Razorpay vault tokenisation).</summary>
[ApiController]
[Route("api/v1/payment/card-tokens")]
[Authorize]
public sealed class CardTokenController(ICardTokenService cardTokenService) : ControllerBase
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    // ── Endpoints ─────────────────────────────────────────────────────────────

    /// <summary>
    /// ENH-PAY-006 — Save a Razorpay vault token reference after a successful first charge.
    /// The request body must contain only the opaque token_id + display-safe metadata;
    /// the server never accepts PAN or CVV.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveToken(
        [FromBody] SaveCardTokenRequest request, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        try
        {
            var dto = await cardTokenService.SaveTokenAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetToken), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// ENH-PAY-006 — List all saved card tokens for the authenticated user.
    /// Tokens are ordered: default first, then newest first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserTokens(CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        var tokens = await cardTokenService.GetUserTokensAsync(userId, ct);
        return Ok(tokens);
    }

    /// <summary>ENH-PAY-006 — Retrieve a single saved card token (must belong to the caller).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetToken(Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        var token = await cardTokenService.GetTokenAsync(userId, id, ct);
        return token is null ? NotFound(new { message = $"Card token {id} not found." }) : Ok(token);
    }

    /// <summary>
    /// ENH-PAY-006 — Remove a saved card token. If the deleted card was the user's default,
    /// the most-recently-saved remaining token is automatically promoted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteToken(Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        try
        {
            await cardTokenService.DeleteTokenAsync(userId, id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// ENH-PAY-006 — Set a saved card as the user's default checkout card.
    /// All other tokens have their IsDefault flag cleared atomically.
    /// </summary>
    [HttpPut("{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        try
        {
            await cardTokenService.SetDefaultAsync(userId, id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
