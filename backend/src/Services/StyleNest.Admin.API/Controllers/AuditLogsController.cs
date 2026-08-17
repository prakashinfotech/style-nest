using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

/// <summary>ENH-ADMIN-001 — Read-only access to the append-only audit trail.</summary>
[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AuditLogPagedResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] Guid?   actorId,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await auditLogService.GetPagedAsync(action, entityType, actorId, page, pageSize, ct);
        return Ok(result);
    }
}
