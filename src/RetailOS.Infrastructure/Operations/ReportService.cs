using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Reports.DTOs;
using RetailOS.Application.Reports.Interfaces;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;

    public ReportService(AppDbContext context, IStoreContext storeContext)
    {
        _context = context;
        _storeContext = storeContext;
    }

    private void EnsureStore()
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");
    }

    public async Task<SalesSummaryReportResponse> GetSalesSummaryAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        Guid? customerId = null,
        string? paymentMethod = null,
        string? basis = "Accrual",
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var query = _context.Sales.AsNoTracking()
            .Include(s => s.LineItems).ThenInclude(li => li.Product).ThenInclude(p => p.Category)
            .Include(s => s.Returns)
            .Where(s => s.Status == SaleStatus.Completed)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.SaleDate >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.SaleDate <= to.Value);

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(paymentMethod) && Enum.TryParse<SalePaymentMethod>(paymentMethod, true, out var pm))
            query = query.Where(s => s.PaymentMethod == pm);

        var sales = await query.ToListAsync(cancellationToken);

        var isCashBasis = string.Equals(basis, "Cash", StringComparison.OrdinalIgnoreCase);

        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;
        decimal totalReturns = 0m;

        foreach (var s in sales)
        {
            grossSales += isCashBasis ? s.CashAmount : s.TotalAmount;
            totalDiscounts += s.DiscountAmount;
            totalReturns += s.Returns.Sum(r => r.TotalAmount);
        }

        var netSales = Math.Max(0m, grossSales - totalReturns);
        var totalOrders = sales.Count;
        var avgOrderValue = totalOrders > 0 ? Math.Round(netSales / totalOrders, 2) : 0m;

        // Payment Method Breakdown
        var paymentGroup = sales
            .GroupBy(s => s.PaymentMethod)
            .Select(g =>
            {
                var amount = g.Sum(x => x.TotalAmount);
                var pct = grossSales > 0 ? Math.Round((amount / grossSales) * 100m, 2) : 0m;
                return new SalesByPaymentMethodResponse(g.Key.ToString(), amount, g.Count(), pct);
            })
            .ToList();

        // Top Selling Products
        var allLineItems = sales.SelectMany(s => s.LineItems).ToList();
        var topProducts = allLineItems
            .GroupBy(li => li.ProductId)
            .Select(g =>
            {
                var first = g.First();
                var qty = g.Sum(x => x.Quantity);
                var rev = g.Sum(x => x.SubTotal);
                var cost = g.Sum(x => x.TotalCost);
                return new TopSellingProductResponse(
                    g.Key,
                    first.Product.Name,
                    first.Product.Barcode,
                    first.Product.Category?.Name ?? "Uncategorized",
                    qty,
                    rev,
                    cost,
                    rev - cost);
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(50)
            .ToList();

        // Category Breakdown
        var categoryBreakdown = allLineItems
            .GroupBy(li => li.Product.CategoryId)
            .Select(g =>
            {
                var first = g.First();
                var qty = g.Sum(x => x.Quantity);
                var rev = g.Sum(x => x.SubTotal);
                var pct = grossSales > 0 ? Math.Round((rev / grossSales) * 100m, 2) : 0m;
                return new SalesByCategoryResponse(
                    g.Key,
                    first.Product.Category?.Name ?? "Uncategorized",
                    qty,
                    rev,
                    pct);
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        return new SalesSummaryReportResponse(
            from,
            to,
            isCashBasis ? "Cash" : "Accrual",
            grossSales,
            totalDiscounts,
            totalReturns,
            netSales,
            totalOrders,
            avgOrderValue,
            paymentGroup,
            topProducts,
            categoryBreakdown);
    }

    public async Task<ProfitLossReportResponse> GetProfitLossAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? basis = "Accrual",
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var isCashBasis = string.Equals(basis, "Cash", StringComparison.OrdinalIgnoreCase);

        var salesQuery = _context.Sales.AsNoTracking()
            .Include(s => s.LineItems)
            .Include(s => s.Returns)
            .Where(s => s.Status == SaleStatus.Completed)
            .AsQueryable();

        var expensesQuery = _context.Expenses.AsNoTracking()
            .Include(e => e.Category)
            .AsQueryable();

        if (from.HasValue)
        {
            salesQuery = salesQuery.Where(s => s.SaleDate >= from.Value);
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate >= from.Value);
        }

        if (to.HasValue)
        {
            salesQuery = salesQuery.Where(s => s.SaleDate <= to.Value);
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate <= to.Value);
        }

        var sales = await salesQuery.ToListAsync(cancellationToken);
        var expenses = await expensesQuery.ToListAsync(cancellationToken);

        decimal grossSales = 0m;
        decimal salesReturns = 0m;
        decimal cogs = 0m;

        foreach (var s in sales)
        {
            grossSales += isCashBasis ? s.CashAmount : s.TotalAmount;
            salesReturns += s.Returns.Sum(r => r.TotalAmount);

            var saleCost = s.TotalCost;
            var returnCost = s.Returns.Sum(r => r.TotalCost);
            cogs += Math.Max(0m, saleCost - returnCost);
        }

        var netSalesRevenue = Math.Max(0m, grossSales - salesReturns);
        var grossProfit = netSalesRevenue - cogs;
        var grossMarginPct = netSalesRevenue > 0 ? Math.Round((grossProfit / netSalesRevenue) * 100m, 2) : 0m;

        var totalExpenses = expenses.Sum(e => e.Amount);
        var netProfit = grossProfit - totalExpenses;
        var netMarginPct = netSalesRevenue > 0 ? Math.Round((netProfit / netSalesRevenue) * 100m, 2) : 0m;

        var expenseBreakdown = expenses
            .GroupBy(e => e.CategoryId)
            .Select(g =>
            {
                var amt = g.Sum(x => x.Amount);
                var pct = totalExpenses > 0 ? Math.Round((amt / totalExpenses) * 100m, 2) : 0m;
                return new ExpenseCategoryBreakdownResponse(
                    g.Key,
                    g.First().Category.Name,
                    amt,
                    pct);
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        return new ProfitLossReportResponse(
            from,
            to,
            isCashBasis ? "Cash" : "Accrual",
            grossSales,
            salesReturns,
            netSalesRevenue,
            cogs,
            grossProfit,
            grossMarginPct,
            totalExpenses,
            netProfit,
            netMarginPct,
            expenseBreakdown);
    }

    public async Task<InventoryValuationReportResponse> GetInventoryValuationAsync(
        DateTimeOffset? asOfDate = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var productsQuery = _context.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
            productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);

        var products = await productsQuery.ToListAsync(cancellationToken);
        var productIds = products.Select(p => p.Id).ToList();

        Dictionary<Guid, decimal> stocksByProduct;

        if (asOfDate.HasValue)
        {
            // Reconstruct point-in-time stock from ledger transactions
            stocksByProduct = await _context.InventoryTransactions.AsNoTracking()
                .Where(it => productIds.Contains(it.ProductId) && it.CreatedAt <= asOfDate.Value)
                .GroupBy(it => it.ProductId)
                .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);
        }
        else
        {
            stocksByProduct = await _context.InventoryTransactions.AsNoTracking()
                .Where(it => productIds.Contains(it.ProductId))
                .GroupBy(it => it.ProductId)
                .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);
        }

        var items = new List<ProductValuationItemResponse>();
        decimal totalValuation = 0m;
        decimal totalUnits = 0m;

        foreach (var p in products)
        {
            var stock = stocksByProduct.TryGetValue(p.Id, out var s) ? s : 0m;
            var wac = p.PurchaseCost ?? 0m;
            var val = stock * wac;
            var potRev = stock * p.SellingPrice;

            totalValuation += val;
            totalUnits += stock;

            items.Add(new ProductValuationItemResponse(
                p.Id,
                p.Name,
                p.Barcode,
                p.Category?.Name ?? "Uncategorized",
                stock,
                wac,
                val,
                p.SellingPrice,
                potRev));
        }

        return new InventoryValuationReportResponse(
            asOfDate ?? DateTimeOffset.UtcNow,
            totalValuation,
            products.Count,
            totalUnits,
            items.OrderByDescending(x => x.TotalValue).ToList());
    }

    public async Task<ProductStockMovementReportResponse> GetProductStockMovementAsync(
        Guid productId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var product = await _context.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
            throw new NotFoundException("PRODUCT_NOT_FOUND", "Product not found.");

        var query = _context.InventoryTransactions.AsNoTracking()
            .Where(it => it.ProductId == productId)
            .OrderBy(it => it.CreatedAt)
            .AsQueryable();

        var allTransactions = await query.ToListAsync(cancellationToken);

        decimal runningBalance = 0m;
        var movementItems = new List<StockMovementItemResponse>();

        foreach (var tx in allTransactions)
        {
            runningBalance += tx.Quantity;

            // Apply date range filter after computing correct running balance
            if (from.HasValue && tx.CreatedAt < from.Value) continue;
            if (to.HasValue && tx.CreatedAt > to.Value) continue;

            movementItems.Add(new StockMovementItemResponse(
                tx.Id,
                tx.CreatedAt,
                tx.Reason.ToString(),
                tx.Quantity,
                tx.CostPerUnit,
                runningBalance,
                tx.ReferenceId,
                tx.Notes));
        }

        return new ProductStockMovementReportResponse(
            product.Id,
            product.Name,
            product.Barcode,
            runningBalance,
            movementItems);
    }

    public async Task<IReadOnlyList<LowStockAlertItemResponse>> GetLowStockAlertsAsync(
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var query = _context.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.MinStockLevel.HasValue && p.MinStockLevel.Value > 0)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var products = await query.ToListAsync(cancellationToken);
        var productIds = products.Select(p => p.Id).ToList();

        var stocks = await _context.InventoryTransactions.AsNoTracking()
            .Where(it => productIds.Contains(it.ProductId))
            .GroupBy(it => it.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

        var alerts = new List<LowStockAlertItemResponse>();

        foreach (var p in products)
        {
            var stock = stocks.TryGetValue(p.Id, out var s) ? s : 0m;
            var minLevel = p.MinStockLevel!.Value;

            if (stock <= minLevel)
            {
                alerts.Add(new LowStockAlertItemResponse(
                    p.Id,
                    p.Name,
                    p.Barcode,
                    p.Category?.Name ?? "Uncategorized",
                    stock,
                    minLevel,
                    Math.Max(0m, minLevel - stock)));
            }
        }

        return alerts.OrderBy(x => x.CurrentStock).ToList();
    }

    public async Task<CustomerBalancesReportResponse> GetCustomerBalancesAsync(
        bool hasBalanceOnly = true,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var customers = await _context.Customers.AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        var customerIds = customers.Select(c => c.Id).ToList();

        var balances = await _context.CustomerAccountTransactions.AsNoTracking()
            .Where(t => customerIds.Contains(t.CustomerId))
            .GroupBy(t => t.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Balance = g.Sum(x => x.Amount),
                LastDate = g.Max(x => (DateTimeOffset?)x.CreatedAt)
            })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        var debtors = new List<CustomerBalanceItemResponse>();
        decimal totalReceivables = 0m;

        foreach (var c in customers)
        {
            var b = balances.TryGetValue(c.Id, out var val) ? val.Balance : 0m;
            var lastDate = balances.TryGetValue(c.Id, out var val2) ? val2.LastDate : null;

            if (hasBalanceOnly && b == 0m) continue;

            if (b > 0m)
                totalReceivables += b;

            debtors.Add(new CustomerBalanceItemResponse(
                c.Id,
                c.Name,
                c.Phone,
                b,
                c.CreditLimit,
                lastDate));
        }

        return new CustomerBalancesReportResponse(
            totalReceivables,
            debtors.Count,
            debtors.OrderByDescending(x => x.CurrentBalance).ToList());
    }

    public async Task<SupplierBalancesReportResponse> GetSupplierBalancesAsync(
        bool hasBalanceOnly = true,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var suppliers = await _context.Suppliers.AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        var supplierIds = suppliers.Select(s => s.Id).ToList();

        var balances = await _context.SupplierAccountTransactions.AsNoTracking()
            .Where(t => supplierIds.Contains(t.SupplierId))
            .GroupBy(t => t.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                Balance = g.Sum(x => x.Amount),
                LastDate = g.Max(x => (DateTimeOffset?)x.CreatedAt)
            })
            .ToDictionaryAsync(x => x.SupplierId, cancellationToken);

        var creditors = new List<SupplierBalanceItemResponse>();
        decimal totalPayables = 0m;

        foreach (var s in suppliers)
        {
            var b = balances.TryGetValue(s.Id, out var val) ? val.Balance : 0m;
            var lastDate = balances.TryGetValue(s.Id, out var val2) ? val2.LastDate : null;

            if (hasBalanceOnly && b == 0m) continue;

            if (b > 0m)
                totalPayables += b;

            creditors.Add(new SupplierBalanceItemResponse(
                s.Id,
                s.Name,
                s.Phone,
                b,
                lastDate));
        }

        return new SupplierBalancesReportResponse(
            totalPayables,
            creditors.Count,
            creditors.OrderByDescending(x => x.CurrentBalance).ToList());
    }

    public async Task<CashRegisterAuditReportResponse> GetCashRegisterAuditAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        EnsureStore();

        var query = _context.CashRegisterTransactions.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        var transactions = await query.OrderBy(t => t.CreatedAt).ToListAsync(cancellationToken);

        decimal totalOpeningFloats = 0m;
        decimal totalCashSales = 0m;
        decimal totalCustomerPayments = 0m;
        decimal totalExpenses = 0m;
        decimal totalSupplierPayments = 0m;
        decimal totalRefunds = 0m;
        decimal totalDiscrepancies = 0m;

        foreach (var tx in transactions)
        {
            switch (tx.Type)
            {
                case CashTransactionType.OpeningFloat:
                    totalOpeningFloats += tx.Amount;
                    break;
                case CashTransactionType.CashSale:
                    totalCashSales += tx.Amount;
                    break;
                case CashTransactionType.CashPaymentIn:
                    totalCustomerPayments += tx.Amount;
                    break;
                case CashTransactionType.CashExpense:
                    totalExpenses += Math.Abs(tx.Amount);
                    break;
                case CashTransactionType.CashPaymentOut:
                    if (tx.Notes?.Contains("Refund", StringComparison.OrdinalIgnoreCase) == true)
                        totalRefunds += Math.Abs(tx.Amount);
                    else
                        totalSupplierPayments += Math.Abs(tx.Amount);
                    break;
                case CashTransactionType.CashAdjustment:
                    totalDiscrepancies += tx.Amount;
                    break;
            }
        }

        var netCashChange = transactions.Sum(t => t.Amount);

        // Daily Groupings
        var dailySummaries = transactions
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g =>
            {
                var floatAmt = g.Where(x => x.Type == CashTransactionType.OpeningFloat).Sum(x => x.Amount);
                var inflows = g.Where(x => x.Amount > 0 && x.Type != CashTransactionType.OpeningFloat).Sum(x => x.Amount);
                var outflows = g.Where(x => x.Amount < 0 && x.Type != CashTransactionType.CashAdjustment).Sum(x => Math.Abs(x.Amount));
                var expected = floatAmt + inflows - outflows;
                var adj = g.Where(x => x.Type == CashTransactionType.CashAdjustment).Sum(x => (decimal?)x.Amount);
                var counted = adj.HasValue ? expected + adj.Value : (decimal?)null;

                return new DailyCashRegisterSummaryResponse(
                    g.Key,
                    floatAmt,
                    inflows,
                    outflows,
                    expected,
                    counted,
                    adj);
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        return new CashRegisterAuditReportResponse(
            from,
            to,
            totalOpeningFloats,
            totalCashSales,
            totalCustomerPayments,
            totalExpenses,
            totalSupplierPayments,
            totalRefunds,
            totalDiscrepancies,
            netCashChange,
            dailySummaries);
    }
}
