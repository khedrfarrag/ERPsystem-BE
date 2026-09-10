# Data Models & DTOs: Phase 5 — Dashboard & Real-Time KPIs

## Overview
The Dashboard module is purely read-oriented and aggregates data from existing transactional entities (`Sale`, `SaleItem`, `Expense`, `Product`, `Customer`, `Supplier`, `CashRegisterSession`, `CashMovement`). No new database tables are created. All models below are DTOs and query transfer contracts located in `RetailOS.Application.Dashboard`.

---

## DTO Models

### 1. `DashboardSummaryDto`
Consolidated high-level store pulse cards.

```csharp
namespace RetailOS.Application.Dashboard.DTOs;

public record DashboardSummaryDto
{
    // Sales KPIs (Today)
    public decimal TodaySalesRevenue { get; init; }
    public int TodayOrdersCount { get; init; }
    public decimal TodayCashSales { get; init; }
    public decimal TodayCreditSales { get; init; }
    
    // Profitability Indicators (Today & Month-to-Date)
    public decimal TodayGrossProfit { get; init; }
    public decimal TodayExpenses { get; init; }
    public decimal TodayOperatingProfit { get; init; }
    public decimal MonthToDateSalesRevenue { get; init; }
    public decimal MonthToDateOperatingProfit { get; init; }

    // Cash Register & Liquidity
    public bool IsCashRegisterOpen { get; init; }
    public decimal LiveCashDrawerBalance { get; init; }
    public string? ActiveRegisterCashierName { get; init; }

    // Outstanding Balances & Working Capital
    public decimal TotalReceivables { get; init; } // Due from customers
    public decimal TotalPayables { get; init; }    // Due to suppliers
    
    // Inventory Alerts
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
}
```

---

### 2. `SalesTrendDto` & `SalesTrendPointDto`
Time-series data points for sales and profit trend charts.

```csharp
namespace RetailOS.Application.Dashboard.DTOs;

public record SalesTrendDto
{
    public int TotalDays { get; init; }
    public decimal TotalPeriodRevenue { get; init; }
    public decimal TotalPeriodProfit { get; init; }
    public int TotalPeriodOrders { get; init; }
    public IReadOnlyList<SalesTrendPointDto> Points { get; init; } = Array.Empty<SalesTrendPointDto>();
}

public record SalesTrendPointDto
{
    public string Date { get; init; } = string.Empty; // YYYY-MM-DD
    public decimal Revenue { get; init; }
    public decimal GrossProfit { get; init; }
    public int OrdersCount { get; init; }
}
```

---

### 3. `TopProductDto`
Ranked best-selling products by quantity and revenue.

```csharp
namespace RetailOS.Application.Dashboard.DTOs;

public record TopProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal QuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal CurrentStock { get; init; }
}
```

---

### 4. `SlowMovingProductDto`
Stagnant products tying up working capital.

```csharp
namespace RetailOS.Application.Dashboard.DTOs;

public record SlowMovingProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal CurrentStock { get; init; }
    public decimal UnitCost { get; init; }
    public decimal TiedUpCapital { get; init; } // CurrentStock * UnitCost
    public DateTime? LastSaleDate { get; init; }
    public int DaysSinceLastSale { get; init; }
}
```

---

### 5. `LowStockAlertDto`
Actionable inventory shortage alerts.

```csharp
namespace RetailOS.Application.Dashboard.DTOs;

public record LowStockAlertDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal CurrentStock { get; init; }
    public decimal MinStockLevel { get; init; }
    public decimal DeficitQuantity { get; init; } // MinStockLevel - CurrentStock (if > 0)
    public bool IsOutOfStock => CurrentStock <= 0;
}
```

---

### 6. `RecentActivityDto`
Live stream of latest operations.

```csharp
namespace RetailOS.Application.Dashboard.DTOs;

public record RecentActivityDto
{
    public Guid Id { get; init; }
    public string ActivityType { get; init; } = string.Empty; // "SALE", "PURCHASE", "EXPENSE", "PAYMENT"
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; }
    public string PerformedBy { get; init; } = string.Empty;
}
```

---

## Service Interface: `IDashboardService`

```csharp
namespace RetailOS.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<SalesTrendDto> GetSalesTrendAsync(int days = 7, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int days = 30, int limit = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(int days = 30, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockAlertDto>> GetLowStockAlertsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(int limit = 10, CancellationToken cancellationToken = default);
}
```
