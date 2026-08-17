namespace StyleNest.Admin.API.DTOs;

public sealed record AuditLogDto(
    Guid     Id,
    DateTime Timestamp,
    Guid?    ActorId,
    string?  ActorName,
    string   Action,
    string   EntityType,
    string?  EntityId,
    string?  OldValue,
    string?  NewValue,
    string?  IpAddress,
    string   RetentionCategory,
    DateTime ExpiresAt);

public sealed record AuditLogPagedResult(
    IReadOnlyList<AuditLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>Carrier for writing an audit entry — avoids 10-param method signatures.</summary>
public sealed record AuditLogEntry(
    string   Action,
    string   EntityType,
    string?  EntityId      = null,
    string?  OldValue      = null,
    string?  NewValue      = null,
    Guid?    ActorId       = null,
    string?  ActorName     = null,
    string?  IpAddress     = null,
    bool     IsFinancial   = false);
