using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Common;

namespace RetailOS.Infrastructure.Persistence.Interceptors;

public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IStoreContext _storeContext;

    public TenantSaveChangesInterceptor(IStoreContext storeContext)
    {
        _storeContext = storeContext;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var now = DateTime.UtcNow;
        var currentStoreId = _storeContext.CurrentStoreId;

        // 1. Update BaseEntity timestamps
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        // 2. Validate and enforce ITenantEntity StoreId
        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.StoreId == Guid.Empty)
                {
                    if (!currentStoreId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Cannot create multi-tenant entity '{entry.Entity.GetType().Name}' without an active StoreContext or explicit StoreId.");
                    }

                    entry.Entity.StoreId = currentStoreId.Value;
                }
                else if (currentStoreId.HasValue && entry.Entity.StoreId != currentStoreId.Value)
                {
                    throw new InvalidOperationException(
                        $"Cross-tenant write attempt detected! Entity StoreId '{entry.Entity.StoreId}' does not match context StoreId '{currentStoreId.Value}'.");
                }
            }
            else if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                if (currentStoreId.HasValue && entry.Entity.StoreId != currentStoreId.Value)
                {
                    throw new InvalidOperationException(
                        $"Cross-tenant modification attempt detected! Entity StoreId '{entry.Entity.StoreId}' does not match context StoreId '{currentStoreId.Value}'.");
                }
            }
        }
    }
}
