using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using StyleNest.User.API.Services;

namespace StyleNest.User.API.Controllers;

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>ENH-NOTIF-002 — Register or refresh an FCM device token.</summary>
public sealed record RegisterFcmTokenRequest(
    [Required, MaxLength(256)] string DeviceId,
    [Required, MaxLength(4096)] string Token,
    [Required, MaxLength(50)]  string Platform);

// ── Controller ────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/v1/users/me/notifications")]
[Authorize]
public class NotificationsController(
    INotificationService notificationService,
    IFcmNotificationService fcmService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false)
    {
        var notifications = await notificationService.GetNotificationsAsync(UserId, page, pageSize, unreadOnly);
        return Ok(notifications);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await notificationService.MarkReadAsync(UserId, id);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await notificationService.MarkAllReadAsync(UserId);
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await notificationService.GetUnreadCountAsync(UserId);
        return Ok(new { Count = count });
    }

    /// <summary>
    /// ENH-NOTIF-002 — Registers or refreshes an FCM device token for the authenticated user.
    /// The client should call this on app start and whenever FCM delivers a new token.
    /// </summary>
    [HttpPost("fcm-token")]
    public async Task<IActionResult> RegisterFcmToken(
        [FromBody] RegisterFcmTokenRequest req,
        CancellationToken ct)
    {
        await fcmService.RegisterTokenAsync(UserId, req.DeviceId, req.Token, req.Platform, ct);
        return NoContent();
    }
}
