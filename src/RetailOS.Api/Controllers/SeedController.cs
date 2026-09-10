using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SeedController : ControllerBase
{
    private readonly IDemoDataSeeder _seeder;
    private readonly AppDbContext _context;

    public SeedController(IDemoDataSeeder seeder, AppDbContext context)
    {
        _seeder = seeder;
        _context = context;
    }

    [HttpPost("demo-store")]
    [ProducesResponseType(typeof(ApiResponse<DemoSeedResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedDemoStore([FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        var result = await _seeder.SeedDemoDataAsync(force, cancellationToken);
        return Ok(ApiResponse<DemoSeedResult>.Ok(result, result.Message));
    }

    [HttpPost("wipe-operational-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> WipeOperationalData(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var sql = @"
            TRUNCATE TABLE 
                b2b_order_items,
                b2b_orders,
                notifications,
                merchants,
                sale_return_line_items,
                sale_returns,
                sale_line_items,
                sales,
                purchase_return_line_items,
                purchase_returns,
                purchase_line_items,
                purchases,
                customer_account_transactions,
                customers,
                supplier_account_transactions,
                supplier_representatives,
                suppliers,
                inventory_transactions,
                products,
                units,
                categories,
                cash_register_transactions,
                expenses,
                expense_categories,
                payments,
                idempotency_records
            RESTART IDENTITY CASCADE;
        ";

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

        await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == "Merchant")
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<string>.Ok("تم مسح كافة البيانات التشغيلية بنجاح. تم الإبقاء على المتجر وحسابات المستخدمين الأساسية جاهزة للبدء."));
    }
}
