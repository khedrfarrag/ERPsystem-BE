namespace RetailOS.Application.Reports.DTOs;

// --- 1. Sales Reporting DTOs ---

public record SalesSummaryReportResponse(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string AccountingBasis,
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal TotalReturns,
    decimal NetSales,
    int TotalOrders,
    decimal AverageOrderValue,
    IReadOnlyList<SalesByPaymentMethodResponse> PaymentBreakdown,
    IReadOnlyList<TopSellingProductResponse> TopProducts,
    IReadOnlyList<SalesByCategoryResponse> CategoryBreakdown);

public record SalesByPaymentMethodResponse(
    string PaymentMethod,
    decimal TotalAmount,
    int TransactionCount,
    decimal Percentage);

public record TopSellingProductResponse(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string CategoryName,
    decimal QuantitySold,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal TotalProfit);

public record SalesByCategoryResponse(
    Guid CategoryId,
    string CategoryName,
    decimal QuantitySold,
    decimal TotalRevenue,
    decimal Percentage);

// --- 2. Profit & Loss (P&L) DTOs ---

public record ProfitLossReportResponse(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string AccountingBasis,
    decimal GrossSales,
    decimal SalesReturns,
    decimal NetSalesRevenue,
    decimal CostOfGoodsSold,
    decimal GrossProfit,
    decimal GrossProfitMarginPercentage,
    decimal OperatingExpenses,
    decimal NetProfit,
    decimal NetProfitMarginPercentage,
    IReadOnlyList<ExpenseCategoryBreakdownResponse> ExpenseBreakdown);

public record ExpenseCategoryBreakdownResponse(
    Guid CategoryId,
    string CategoryName,
    decimal TotalAmount,
    decimal Percentage);

// --- 3. Inventory & Valuation DTOs ---

public record InventoryValuationReportResponse(
    DateTimeOffset AsOfDate,
    decimal TotalValuation,
    int TotalProductsCount,
    decimal TotalUnitsCount,
    IReadOnlyList<ProductValuationItemResponse> Items);

public record ProductValuationItemResponse(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string CategoryName,
    decimal CurrentStock,
    decimal UnitCostWac,
    decimal TotalValue,
    decimal SellingPrice,
    decimal PotentialRevenue);

public record StockMovementItemResponse(
    Guid TransactionId,
    DateTimeOffset Date,
    string Reason,
    decimal QuantityChange,
    decimal UnitCost,
    decimal ResultingBalance,
    Guid? ReferenceId,
    string? Notes);

public record ProductStockMovementReportResponse(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    decimal CurrentStock,
    IReadOnlyList<StockMovementItemResponse> Movements);

public record LowStockAlertItemResponse(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string CategoryName,
    decimal CurrentStock,
    decimal? MinReorderLevel,
    decimal ShortageQuantity);

// --- 4. Accounts Balances DTOs ---

public record CustomerBalanceItemResponse(
    Guid CustomerId,
    string CustomerName,
    string? Phone,
    decimal CurrentBalance,
    decimal? CreditLimit,
    DateTimeOffset? LastTransactionDate);

public record CustomerBalancesReportResponse(
    decimal TotalReceivables,
    int TotalDebtorsCount,
    IReadOnlyList<CustomerBalanceItemResponse> Debtors);

public record SupplierBalanceItemResponse(
    Guid SupplierId,
    string SupplierName,
    string? Phone,
    decimal CurrentBalance,
    DateTimeOffset? LastTransactionDate);

public record SupplierBalancesReportResponse(
    decimal TotalPayables,
    int TotalCreditorsCount,
    IReadOnlyList<SupplierBalanceItemResponse> Creditors);

// --- 5. Cash Register Audit DTOs ---

public record CashRegisterAuditReportResponse(
    DateTimeOffset? From,
    DateTimeOffset? To,
    decimal TotalOpeningFloats,
    decimal TotalCashSalesInflows,
    decimal TotalCustomerPaymentInflows,
    decimal TotalExpenseOutflows,
    decimal TotalSupplierPaymentOutflows,
    decimal TotalRefundOutflows,
    decimal TotalDiscrepancies,
    decimal NetCashChange,
    IReadOnlyList<DailyCashRegisterSummaryResponse> DailySummaries);

public record DailyCashRegisterSummaryResponse(
    DateTime Date,
    decimal OpeningFloat,
    decimal Inflows,
    decimal Outflows,
    decimal ExpectedClosing,
    decimal? ActualCounted,
    decimal? Discrepancy);
