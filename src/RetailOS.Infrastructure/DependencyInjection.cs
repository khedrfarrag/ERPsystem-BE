using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailOS.Application.Auth.Interfaces;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Auth;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Infrastructure.Persistence.Interceptors;
using RetailOS.Infrastructure.Services;

namespace RetailOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IStoreContext, StoreContext>();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();

            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<RetailOS.Application.Stores.Interfaces.IStoreService, RetailOS.Infrastructure.Stores.StoreService>();
        services.AddScoped<RetailOS.Application.Users.Interfaces.IUserService, RetailOS.Infrastructure.Users.UserService>();
        services.AddScoped<RetailOS.Application.Categories.Interfaces.ICategoryService, RetailOS.Infrastructure.Catalog.CategoryService>();
        services.AddScoped<RetailOS.Application.Units.Interfaces.IUnitService, RetailOS.Infrastructure.Catalog.UnitService>();
        services.AddScoped<RetailOS.Application.Products.Interfaces.IProductService, RetailOS.Infrastructure.Catalog.ProductService>();
        services.AddScoped<RetailOS.Application.Products.Interfaces.IProductImportService, RetailOS.Infrastructure.Catalog.ProductImportService>();
        services.AddScoped<RetailOS.Application.Inventory.Interfaces.IInventoryService, RetailOS.Infrastructure.Inventory.InventoryService>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<RetailOS.Application.Suppliers.Interfaces.ISupplierService, RetailOS.Infrastructure.Operations.SupplierService>();
        services.AddScoped<RetailOS.Application.Customers.Interfaces.ICustomerService, RetailOS.Infrastructure.Operations.CustomerService>();
        services.AddScoped<RetailOS.Application.Purchases.Interfaces.IPurchaseService, RetailOS.Infrastructure.Operations.PurchaseService>();
        services.AddScoped<RetailOS.Application.Sales.Interfaces.ISaleService, RetailOS.Infrastructure.Operations.SaleService>();
        services.AddScoped<RetailOS.Application.Payments.Interfaces.IPaymentService, RetailOS.Infrastructure.Operations.PaymentService>();
        services.AddScoped<RetailOS.Application.Expenses.Interfaces.IExpenseService, RetailOS.Infrastructure.Operations.ExpenseService>();
        services.AddScoped<RetailOS.Application.CashRegister.Interfaces.ICashRegisterService, RetailOS.Infrastructure.Operations.CashRegisterService>();
        services.AddScoped<RetailOS.Application.Reports.Interfaces.IReportService, RetailOS.Infrastructure.Operations.ReportService>();
        services.AddScoped<RetailOS.Application.Dashboard.IDashboardService, RetailOS.Infrastructure.Operations.DashboardService>();
        services.AddScoped<RetailOS.Application.B2B.IMerchantService, RetailOS.Infrastructure.B2B.MerchantService>();
        services.AddScoped<RetailOS.Application.B2B.IB2BOrderService, RetailOS.Infrastructure.B2B.B2BOrderService>();
        services.AddScoped<RetailOS.Application.B2B.INotificationService, RetailOS.Infrastructure.B2B.NotificationService>();
        services.AddScoped<RetailOS.Application.Common.Interfaces.IDemoDataSeeder, RetailOS.Infrastructure.Services.DemoDataSeeder>();

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }
}
