using System.Globalization;
using System.Text;
using RetailOS.Application.Reports.DTOs;

namespace RetailOS.Application.Reports.Common;

public static class CsvExporter
{
    private static string Escape(object? value)
    {
        if (value == null) return "";
        var str = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        if (str.Contains(',') || str.Contains('"') || str.Contains('\n') || str.Contains('\r'))
        {
            return $"\"{str.Replace("\"", "\"\"")}\"";
        }
        return str;
    }

    public static byte[] ExportSalesSummary(SalesSummaryReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Gross Sales,{report.GrossSales}");
        sb.AppendLine($"Total Discounts,{report.TotalDiscounts}");
        sb.AppendLine($"Total Returns,{report.TotalReturns}");
        sb.AppendLine($"Net Sales,{report.NetSales}");
        sb.AppendLine($"Total Orders,{report.TotalOrders}");
        sb.AppendLine($"Average Order Value,{report.AverageOrderValue}");
        sb.AppendLine();

        sb.AppendLine("Top Selling Products");
        sb.AppendLine("Product Name,Barcode,Category,Quantity Sold,Revenue,Cost,Profit");
        foreach (var p in report.TopProducts)
        {
            sb.AppendLine($"{Escape(p.ProductName)},{Escape(p.Barcode)},{Escape(p.CategoryName)},{p.QuantitySold},{p.TotalRevenue},{p.TotalCost},{p.TotalProfit}");
        }
        sb.AppendLine();

        sb.AppendLine("Sales by Payment Method");
        sb.AppendLine("Payment Method,Total Amount,Transaction Count,Percentage");
        foreach (var pm in report.PaymentBreakdown)
        {
            sb.AppendLine($"{Escape(pm.PaymentMethod)},{pm.TotalAmount},{pm.TransactionCount},{pm.Percentage}%");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] ExportInventoryValuation(InventoryValuationReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Product Name,Barcode,Category,Current Stock,Unit Cost (WAC),Total Value,Selling Price,Potential Revenue");
        foreach (var item in report.Items)
        {
            sb.AppendLine($"{Escape(item.ProductName)},{Escape(item.Barcode)},{Escape(item.CategoryName)},{item.CurrentStock},{item.UnitCostWac},{item.TotalValue},{item.SellingPrice},{item.PotentialRevenue}");
        }
        sb.AppendLine();
        sb.AppendLine($"Total Valuation,{report.TotalValuation}");
        sb.AppendLine($"Total Products,{report.TotalProductsCount}");
        sb.AppendLine($"Total Units,{report.TotalUnitsCount}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] ExportStockMovement(ProductStockMovementReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Stock Movement History for: {report.ProductName} (Barcode: {report.Barcode ?? "N/A"})");
        sb.AppendLine($"Current Stock: {report.CurrentStock}");
        sb.AppendLine();
        sb.AppendLine("Date,Reason,Quantity Change,Unit Cost,Resulting Balance,Notes");
        foreach (var m in report.Movements)
        {
            sb.AppendLine($"{m.Date:yyyy-MM-dd HH:mm:ss},{Escape(m.Reason)},{m.QuantityChange},{m.UnitCost},{m.ResultingBalance},{Escape(m.Notes)}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] ExportCustomerBalances(CustomerBalancesReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Customer Name,Phone,Current Balance,Credit Limit,Last Transaction Date");
        foreach (var d in report.Debtors)
        {
            sb.AppendLine($"{Escape(d.CustomerName)},{Escape(d.Phone)},{d.CurrentBalance},{d.CreditLimit},{d.LastTransactionDate:yyyy-MM-dd HH:mm:ss}");
        }
        sb.AppendLine();
        sb.AppendLine($"Total Receivables,{report.TotalReceivables}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] ExportSupplierBalances(SupplierBalancesReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Supplier Name,Phone,Current Balance,Last Transaction Date");
        foreach (var c in report.Creditors)
        {
            sb.AppendLine($"{Escape(c.SupplierName)},{Escape(c.Phone)},{c.CurrentBalance},{c.LastTransactionDate:yyyy-MM-dd HH:mm:ss}");
        }
        sb.AppendLine();
        sb.AppendLine($"Total Payables,{report.TotalPayables}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] ExportCashRegisterAudit(CashRegisterAuditReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Opening Float,Inflows,Outflows,Expected Closing,Actual Counted,Discrepancy");
        foreach (var d in report.DailySummaries)
        {
            sb.AppendLine($"{d.Date:yyyy-MM-dd},{d.OpeningFloat},{d.Inflows},{d.Outflows},{d.ExpectedClosing},{d.ActualCounted},{d.Discrepancy}");
        }
        sb.AppendLine();
        sb.AppendLine($"Net Cash Change,{report.NetCashChange}");
        sb.AppendLine($"Total Discrepancies,{report.TotalDiscrepancies}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
