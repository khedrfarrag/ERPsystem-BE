using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Common;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    private readonly IStoreContext _storeContext;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IStoreContext storeContext)
        : base(options)
    {
        _storeContext = storeContext;
    }

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierRepresentative> SupplierRepresentatives => Set<SupplierRepresentative>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SupplierAccountTransaction> SupplierAccountTransactions => Set<SupplierAccountTransaction>();
    public DbSet<CustomerAccountTransaction> CustomerAccountTransactions => Set<CustomerAccountTransaction>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseLineItem> PurchaseLineItems => Set<PurchaseLineItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnLineItem> PurchaseReturnLineItems => Set<PurchaseReturnLineItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLineItem> SaleLineItems => Set<SaleLineItem>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnLineItem> SaleReturnLineItems => Set<SaleReturnLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashRegisterTransaction> CashRegisterTransactions => Set<CashRegisterTransaction>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<B2BOrder> B2BOrders => Set<B2BOrder>();
    public DbSet<B2BOrderItem> B2BOrderItems => Set<B2BOrderItem>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Per Constitution: All monetary and price decimals default to numeric(19,4)
        configurationBuilder.Properties<decimal>()
            .HavePrecision(19, 4);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration classes in this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure Global Query Filter for ITenantEntity and SoftDeletableEntity implementations
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenant = typeof(ITenantEntity).IsAssignableFrom(clrType);
            var isSoftDelete = typeof(SoftDeletableEntity).IsAssignableFrom(clrType);

            if (isTenant && isSoftDelete)
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureTenantAndSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, new object[] { builder });
            }
            else if (isTenant)
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureTenantOnlyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, new object[] { builder });
            }
            else if (isSoftDelete)
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureSoftDeleteOnlyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, new object[] { builder });
            }
        }
    }

    private void ConfigureTenantAndSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : SoftDeletableEntity, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            (!_storeContext.HasStore || e.StoreId == _storeContext.CurrentStoreId) && e.DeletedAt == null);
    }

    private void ConfigureTenantOnlyFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            !_storeContext.HasStore || e.StoreId == _storeContext.CurrentStoreId);
    }

    private void ConfigureSoftDeleteOnlyFilter<TEntity>(ModelBuilder builder)
        where TEntity : SoftDeletableEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
