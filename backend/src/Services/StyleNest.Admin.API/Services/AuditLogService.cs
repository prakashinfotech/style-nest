using Microsoft.EntityFrameworkCore;
using StyleNest.Admin.API.DTOs;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Admin.API.Services;

public interface IAuditLogService
{
    /// <summary>ENH-ADMIN-001 — Append an immutable audit entry. Never throws; swallows errors to not block callers.</summary>
    Task LogAsync(AuditLogEntry entry, CancellationToken ct = default);

    /// <summary>Paginated query for admin UI, filterable by action / entityType / actorId.</summary>
    Task<AuditLogPagedResult> GetPagedAsync(
        string? action,
        string? entityType,
        Guid?   actorId,
        int     page,
        int     pageSize,
        CancellationToken ct = default);
}

/// <summary>
/// ENH-ADMIN-001 — Append-only audit log.
/// Financial records (IsFinancial=true): ExpiresAt = Timestamp + 7 years.
/// Non-financial records: ExpiresAt = Timestamp + 3 years.
/// </summary>
public sealed class AuditLogService(
    AppDbContext db,
    ILogger<AuditLogService> logger) : IAuditLogService
{
    public async Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        try
        {
            var now      = DateTime.UtcNow;
            var category = entry.IsFinancial ? RetentionCategory.Financial : RetentionCategory.NonFinancial;
            var expiresAt = entry.IsFinancial ? now.AddYears(7) : now.AddYears(3);

            db.AuditLogs.Add(new AuditLog
            {
                Id                = Guid.NewGuid(),
                Timestamp         = now,
                ActorId           = entry.ActorId,
                ActorName         = entry.ActorName,
                Action            = entry.Action,
                EntityType        = entry.EntityType,
                EntityId          = entry.EntityId,
                OldValue          = entry.OldValue,
                NewValue          = entry.NewValue,
                IpAddress         = entry.IpAddress,
                RetentionCategory = category,
                ExpiresAt         = expiresAt,
            });

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit logging must not break the calling flow
            logger.LogError(ex, "AuditLog write failed for Action={Action} EntityType={EntityType}",
                entry.Action, entry.EntityType);
        }
    }

    public async Task<AuditLogPagedResult> GetPagedAsync(
        string? action,
        string? entityType,
        Guid?   actorId,
        int     page,
        int     pageSize,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (actorId.HasValue)
            query = query.Where(a => a.ActorId == actorId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.Timestamp, a.ActorId, a.ActorName,
                a.Action, a.EntityType, a.EntityId,
                a.OldValue, a.NewValue, a.IpAddress,
                a.RetentionCategory.ToString(), a.ExpiresAt))
            .ToListAsync(ct);

        return new AuditLogPagedResult(items, total, page, pageSize);
    }
}
