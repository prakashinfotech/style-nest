using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Persistence;

public sealed class SaveChangesAuditInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity<Guid> entity)
            {
                if (entry.State == EntityState.Added)
                    entity.CreatedAt = now;

                if (entry.State is EntityState.Added or EntityState.Modified)
                    entity.UpdatedAt = now;
            }

            // ApplicationUser does not extend BaseEntity but has its own audit fields
            if (entry.Entity is ApplicationUser user)
            {
                if (entry.State == EntityState.Added)
                    user.CreatedAt = now;

                if (entry.State is EntityState.Added or EntityState.Modified)
                    user.UpdatedAt = now;
            }
        }
    }
}
