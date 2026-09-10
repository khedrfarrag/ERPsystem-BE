using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Dashboard;
using RetailOS.Application.Dashboard.DTOs;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;

    public DashboardService(AppDbContext context, IStoreContext storeContext)
    {
        _context = context;
        _storeContext = storeContext;
    }

    private void EnsureStore()
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var todayUtc = DateTime.UtcNow.Date;
        var startOfMonthUtc = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // 1. Sales today and MTD
        var allSales = await _context.Sales.AsNoTracking()
            .Include(s => s.LineItems)
            .Include(s => s.Returns)
            .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= startOfMonthUtc)
            .ToListAsync(cancellationToken);

        var todaySales = allSales.Where(s => s.SaleDate.UtcDateTime.Date == todayUtc).ToList();

        decimal todaySalesRevenue = 0m;
        decimal todayCashSales = 0m;
        decimal todayCreditSales = 0m;
        decimal todayCogs = 0m;
        decimal todayReturns = 0m;

        foreach (var s in todaySales)
        {
            todaySalesRevenue += s.TotalAmount;
            todayCashSales += s.CashAmount;
            todayCreditSales += s.CreditAmount;
            
            var sCogs = s.LineItems.Sum(li => li.TotalCost);
            var rCost = s.Returns.Sum(r => r.LineItems.Sum(rli => rli.TotalCost));
            todayCogs += Math.Max(0m, sCogs - rCost);
            todayReturns += s.Returns.Sum(r => r.TotalAmount);
        }

        var todayNetSales = Math.Max(0m, todaySalesRevenue - todayReturns);
        var todayGrossProfit = todayNetSales - todayCogs;

        // Today's expenses
        var allExpenses = await _context.Expenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= startOfMonthUtc)
            .ToListAsync(cancellationToken);

        var todayExpenses = allExpenses
            .Where(e => e.ExpenseDate.UtcDateTime.Date == todayUtc)
            .Sum(e => e.Amount);

        var todayOperatingProfit = todayGrossProfit - todayExpenses;

        // Month-to-date (MTD) Calculations
        decimal mtdSalesRevenue = 0m;
        decimal mtdCogs = 0m;
        decimal mtdReturns = 0m;

        foreach (var s in allSales)
        {
            mtdSalesRevenue += s.TotalAmount;
            var sCogs = s.LineItems.Sum(li => li.TotalCost);
            var rCost = s.Returns.Sum(r => r.LineItems.Sum(rli => rli.TotalCost));
            mtdCogs += Math.Max(0m, sCogs - rCost);
            mtdReturns += s.Returns.Sum(r => r.TotalAmount);
        }

        var mtdNetSales = Math.Max(0m, mtdSalesRevenue - mtdReturns);
        var mtdGrossProfit = mtdNetSales - mtdCogs;
        var mtdExpenses = allExpenses.Sum(e => e.Amount);
        var mtdOperatingProfit = mtdGrossProfit - mtdExpenses;

        // 2. Cash Register & Live Drawer Balance
        var liveCashDrawerBalance = await _context.CashRegisterTransactions.AsNoTracking()
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var lastFloat = await _context.CashRegisterTransactions.AsNoTracking()
            .Where(t => t.Type == CashTransactionType.OpeningFloat)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        bool isCashRegisterOpen = lastFloat != null && Math.Abs((DateTime.UtcNow - lastFloat.CreatedAt).TotalHours) <= 24;
        string? activeCashierName = null;
        if (isCashRegisterOpen && lastFloat != null)
        {
            var user = await _context.Users.AsNoTracking()
                .Where(u => u.Id == lastFloat.CreatedBy)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync(cancellationToken);
            activeCashierName = user ?? lastFloat.CreatedBy.ToString();
        }

        // 3. Outstanding Balances (Receivables & Payables)
        var customerBalances = await _context.CustomerAccountTransactions.AsNoTracking()
            .GroupBy(t => t.CustomerId)
            .Select(g => g.Sum(x => x.Amount))
            .ToListAsync(cancellationToken);

        var totalReceivables = customerBalances.Where(b => b > 0m).Sum();

        var supplierBalances = await _context.SupplierAccountTransactions.AsNoTracking()
            .GroupBy(t => t.SupplierId)
            .Select(g => g.Sum(x => x.Amount))
            .ToListAsync(cancellationToken);

        var totalPayables = supplierBalances.Where(b => b > 0m).Sum();

        // 4. Low stock and Out of stock counts
        var activeProducts = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.MinStockLevel })
            .ToListAsync(cancellationToken);

        var productIds = activeProducts.Select(p => p.Id).ToList();
        var stockDict = await _context.InventoryTransactions.AsNoTracking()
            .Where(it => productIds.Contains(it.ProductId))
            .GroupBy(it => it.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

        int lowStockCount = 0;
        int outOfStockCount = 0;

        foreach (var p in activeProducts)
        {
            var stock = stockDict.TryGetValue(p.Id, out var s) ? s : 0m;
            if (stock <= 0m)
            {
                outOfStockCount++;
            }
            if (p.MinStockLevel.HasValue && p.MinStockLevel.Value > 0 && stock <= p.MinStockLevel.Value)
            {
                lowStockCount++;
            }
        }

        return new DashboardSummaryDto(
            TodaySalesRevenue: todaySalesRevenue,
            TodayOrdersCount: todaySales.Count,
            TodayCashSales: todayCashSales,
            TodayCreditSales: todayCreditSales,
            TodayGrossProfit: todayGrossProfit,
            TodayExpenses: todayExpenses,
            TodayOperatingProfit: todayOperatingProfit,
            MonthToDateSalesRevenue: mtdSalesRevenue,
            MonthToDateOperatingProfit: mtdOperatingProfit,
            IsCashRegisterOpen: isCashRegisterOpen,
            LiveCashDrawerBalance: liveCashDrawerBalance,
            ActiveRegisterCashierName: activeCashierName,
            TotalReceivables: totalReceivables,
            TotalPayables: totalPayables,
            LowStockCount: lowStockCount,
            OutOfStockCount: outOfStockCount
        );
    }

    public async Task<SalesTrendDto> GetSalesTrendAsync(
        int days = 7,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        DateTime startUtc;
        DateTime endUtc;

        if (startDate.HasValue && endDate.HasValue)
        {
            startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            endUtc = DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc);
        }
        else
        {
            if (days <= 0) days = 7;
            endUtc = DateTime.UtcNow.Date;
            startUtc = endUtc.AddDays(-(days - 1));
        }

        if (startUtc > endUtc)
        {
            (startUtc, endUtc) = (endUtc, startUtc);
        }

        var endFilterUtc = endUtc.AddDays(1).AddTicks(-1);

        var sales = await _context.Sales.AsNoTracking()
            .Include(s => s.LineItems)
            .Include(s => s.Returns)
            .Where(s => s.Status == SaleStatus.Completed &&
                        s.SaleDate >= startUtc &&
                        s.SaleDate <= endFilterUtc)
            .ToListAsync(cancellationToken);

        // Group by calendar date in C#
        var groupedSales = sales
            .GroupBy(s => s.SaleDate.UtcDateTime.Date)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    decimal rev = 0m;
                    decimal cost = 0m;
                    decimal rets = 0m;

                    foreach (var s in g)
                    {
                        rev += s.TotalAmount;
                        var sCogs = s.LineItems.Sum(li => li.TotalCost);
                        var rCost = s.Returns.Sum(r => r.LineItems.Sum(rli => rli.TotalCost));
                        cost += Math.Max(0m, sCogs - rCost);
                        rets += s.Returns.Sum(r => r.TotalAmount);
                    }

                    var netRev = Math.Max(0m, rev - rets);
                    var profit = netRev - cost;
                    return new { Revenue = netRev, Profit = profit, OrdersCount = g.Count() };
                });

        var points = new List<SalesTrendPointDto>();
        decimal totalPeriodRevenue = 0m;
        decimal totalPeriodProfit = 0m;
        int totalPeriodOrders = 0;

        for (var d = startUtc; d <= endUtc; d = d.AddDays(1))
        {
            var dateStr = d.ToString("yyyy-MM-dd");
            if (groupedSales.TryGetValue(d, out var data))
            {
                points.Add(new SalesTrendPointDto(dateStr, data.Revenue, data.Profit, data.OrdersCount));
                totalPeriodRevenue += data.Revenue;
                totalPeriodProfit += data.Profit;
                totalPeriodOrders += data.OrdersCount;
            }
            else
            {
                points.Add(new SalesTrendPointDto(dateStr, 0m, 0m, 0));
            }
        }

        return new SalesTrendDto(
            TotalDays: points.Count,
            TotalPeriodRevenue: totalPeriodRevenue,
            TotalPeriodProfit: totalPeriodProfit,
            TotalPeriodOrders: totalPeriodOrders,
            Points: points
        );
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(
        int days = 30,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        if (days <= 0) days = 30;
        if (limit <= 0) limit = 5;
        if (limit > 50) limit = 50;

        var cutoffDate = DateTime.UtcNow.Date.AddDays(-days);

        var saleItems = await _context.SaleLineItems.AsNoTracking()
            .Include(si => si.Product).ThenInclude(p => p.Unit)
            .Include(si => si.Sale)
            .Where(si => si.Sale.Status == SaleStatus.Completed && si.Sale.SaleDate >= cutoffDate)
            .ToListAsync(cancellationToken);

        var topGrouped = saleItems
            .GroupBy(si => si.ProductId)
            .Select(g =>
            {
                var first = g.First();
                return new
                {
                    ProductId = g.Key,
                    ProductName = first.Product?.Name ?? "Unknown Product",
                    Barcode = first.Product?.Barcode,
                    UnitName = first.Product?.Unit?.Name ?? string.Empty,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.SubTotal)
                };
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.TotalRevenue)
            .Take(limit)
            .ToList();

        var productIds = topGrouped.Select(x => x.ProductId).ToList();

        var stocks = await _context.InventoryTransactions.AsNoTracking()
            .Where(it => productIds.Contains(it.ProductId))
            .GroupBy(it => it.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

        var result = topGrouped.Select(x => new TopProductDto(
            ProductId: x.ProductId,
            ProductName: x.ProductName,
            Barcode: x.Barcode,
            UnitName: x.UnitName,
            QuantitySold: x.QuantitySold,
            TotalRevenue: x.TotalRevenue,
            CurrentStock: stocks.TryGetValue(x.ProductId, out var s) ? s : 0m
        )).ToList();

        return result;
    }

    public async Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(
        int days = 30,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        if (days <= 0) days = 30;
        if (limit <= 0) limit = 10;
        if (limit > 50) limit = 50;

        var cutoffDate = DateTime.UtcNow.Date.AddDays(-days);

        var activeProducts = await _context.Products.AsNoTracking()
            .Include(p => p.Unit)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var productIds = activeProducts.Select(p => p.Id).ToList();

        // Get current stock
        var stocks = await _context.InventoryTransactions.AsNoTracking()
            .Where(it => productIds.Contains(it.ProductId))
            .GroupBy(it => it.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

        // Find products with sales within cutoff
        var soldProductIdsInWindow = await _context.SaleLineItems.AsNoTracking()
            .Include(si => si.Sale)
            .Where(si => productIds.Contains(si.ProductId) &&
                         si.Sale.Status == SaleStatus.Completed &&
                         si.Sale.SaleDate >= cutoffDate)
            .Select(si => si.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Find last sale date for each product
        var lastSales = await _context.SaleLineItems.AsNoTracking()
            .Include(si => si.Sale)
            .Where(si => productIds.Contains(si.ProductId) && si.Sale.Status == SaleStatus.Completed)
            .GroupBy(si => si.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                LastSaleDate = g.Max(x => (DateTimeOffset?)x.Sale.SaleDate)
            })
            .ToDictionaryAsync(x => x.ProductId, x => x.LastSaleDate, cancellationToken);

        var now = DateTime.UtcNow;
        var slowItems = new List<SlowMovingProductDto>();

        foreach (var p in activeProducts)
        {
            var stock = stocks.TryGetValue(p.Id, out var s) ? s : 0m;
            // Stagnant condition: positive stock and no sales in the specified days window
            if (stock > 0m && !soldProductIdsInWindow.Contains(p.Id))
            {
                var unitCost = p.PurchaseCost ?? 0m;
                var tiedUp = stock * unitCost;
                var lastSaleDate = lastSales.TryGetValue(p.Id, out var lsd) ? lsd : null;
                var daysSinceLastSale = lastSaleDate.HasValue
                    ? (int)Math.Floor((now - lastSaleDate.Value.UtcDateTime).TotalDays)
                    : days;

                slowItems.Add(new SlowMovingProductDto(
                    ProductId: p.Id,
                    ProductName: p.Name,
                    Barcode: p.Barcode,
                    UnitName: p.Unit?.Name ?? string.Empty,
                    CurrentStock: stock,
                    UnitCost: unitCost,
                    TiedUpCapital: tiedUp,
                    LastSaleDate: lastSaleDate,
                    DaysSinceLastSale: daysSinceLastSale
                ));
            }
        }

        return slowItems
            .OrderByDescending(x => x.TiedUpCapital)
            .ThenByDescending(x => x.DaysSinceLastSale)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<LowStockAlertDto>> GetLowStockAlertsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        if (limit <= 0) limit = 10;
        if (limit > 50) limit = 50;

        var products = await _context.Products.AsNoTracking()
            .Include(p => p.Unit)
            .Where(p => p.IsActive && p.MinStockLevel.HasValue && p.MinStockLevel.Value > 0)
            .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.Id).ToList();

        var stocks = await _context.InventoryTransactions.AsNoTracking()
            .Where(it => productIds.Contains(it.ProductId))
            .GroupBy(it => it.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

        var alerts = new List<LowStockAlertDto>();

        foreach (var p in products)
        {
            var stock = stocks.TryGetValue(p.Id, out var s) ? s : 0m;
            var minLevel = p.MinStockLevel!.Value;

            if (stock <= minLevel)
            {
                alerts.Add(new LowStockAlertDto(
                    ProductId: p.Id,
                    ProductName: p.Name,
                    Barcode: p.Barcode,
                    UnitName: p.Unit?.Name ?? string.Empty,
                    CurrentStock: stock,
                    MinStockLevel: minLevel,
                    DeficitQuantity: Math.Max(0m, minLevel - stock),
                    IsOutOfStock: stock <= 0m
                ));
            }
        }

        return alerts
            .OrderBy(x => x.CurrentStock)
            .ThenByDescending(x => x.DeficitQuantity)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        if (limit <= 0) limit = 10;
        if (limit > 50) limit = 50;

        // Fetch recent items across core operational tables
        var recentSales = await _context.Sales.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .Select(s => new RecentActivityDto(
                s.Id,
                "SALE",
                s.InvoiceNumber,
                $"Sale ({s.PaymentMethod})",
                s.TotalAmount,
                s.SaleDate,
                s.CreatedBy.ToString()
            ))
            .ToListAsync(cancellationToken);

        var recentPurchases = await _context.Purchases.AsNoTracking()
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new RecentActivityDto(
                p.Id,
                "PURCHASE",
                p.InvoiceNumber ?? p.PurchaseNumber,
                $"Purchase from {(p.Supplier != null ? p.Supplier.Name : "Supplier")}",
                p.TotalAmount,
                p.PurchaseDate,
                p.CreatedBy.ToString()
            ))
            .ToListAsync(cancellationToken);

        var recentExpenses = await _context.Expenses.AsNoTracking()
            .Include(e => e.Category)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new RecentActivityDto(
                e.Id,
                "EXPENSE",
                e.Id.ToString(),
                e.Description ?? (e.Category != null ? e.Category.Name : "Expense"),
                e.Amount,
                e.ExpenseDate,
                e.CreatedBy.ToString()
            ))
            .ToListAsync(cancellationToken);

        var recentPayments = await _context.Payments.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new RecentActivityDto(
                p.Id,
                "PAYMENT",
                p.ReferenceNumber ?? p.Id.ToString(),
                $"{p.PartyType} Payment ({p.PaymentMethod})",
                p.Amount,
                p.PaymentDate,
                p.CreatedBy.ToString()
            ))
            .ToListAsync(cancellationToken);

        var combined = recentSales
            .Concat(recentPurchases)
            .Concat(recentExpenses)
            .Concat(recentPayments)
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToList();

        return combined;
    }
}
