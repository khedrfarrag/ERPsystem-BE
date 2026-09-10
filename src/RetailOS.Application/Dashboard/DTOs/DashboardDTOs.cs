namespace RetailOS.Application.Dashboard.DTOs;

public record DashboardSummaryDto(
    decimal TodaySalesRevenue,
    int TodayOrdersCount,
    decimal TodayCashSales,
    decimal TodayCreditSales,
    decimal TodayGrossProfit,
    decimal TodayExpenses,
    decimal TodayOperatingProfit,
    decimal MonthToDateSalesRevenue,
    decimal MonthToDateOperatingProfit,
    bool IsCashRegisterOpen,
    decimal LiveCashDrawerBalance,
    string? ActiveRegisterCashierName,
    decimal TotalReceivables,
    decimal TotalPayables,
    int LowStockCount,
    int OutOfStockCount
);

public record SalesTrendDto(
    int TotalDays,
    decimal TotalPeriodRevenue,
    decimal TotalPeriodProfit,
    int TotalPeriodOrders,
    IReadOnlyList<SalesTrendPointDto> Points
);

public record SalesTrendPointDto(
    string Date,
    decimal Revenue,
    decimal GrossProfit,
    int OrdersCount
);

public record TopProductDto(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string UnitName,
    decimal QuantitySold,
    decimal TotalRevenue,
    decimal CurrentStock
);

public record SlowMovingProductDto(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string UnitName,
    decimal CurrentStock,
    decimal UnitCost,
    decimal TiedUpCapital,
    DateTimeOffset? LastSaleDate,
    int DaysSinceLastSale
);

public record LowStockAlertDto(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string UnitName,
    decimal CurrentStock,
    decimal MinStockLevel,
    decimal DeficitQuantity,
    bool IsOutOfStock
);

public record RecentActivityDto(
    Guid Id,
    string ActivityType,
    string ReferenceNumber,
    string Description,
    decimal Amount,
    DateTimeOffset Timestamp,
    string PerformedBy
);
