namespace RetailOS.Application.Sales.DTOs;

public record SaleLineItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount = 0m);

public record CreateSaleRequest(
    Guid? CustomerId,
    string PaymentMethod,
    decimal CashAmount,
    string? Notes,
    List<SaleLineItemRequest> Items);

public record StockShortageItem(
    Guid ProductId,
    string ProductName,
    decimal RequestedQuantity,
    decimal AvailableQuantity);

public record SaleLineItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    decimal Discount,
    decimal SubTotal,
    decimal TotalCost);

public record SaleResponse(
    Guid Id,
    Guid? CustomerId,
    string? CustomerName,
    string InvoiceNumber,
    DateTimeOffset SaleDate,
    string Status,
    string PaymentMethod,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal CashAmount,
    decimal CreditAmount,
    decimal TotalCost,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SaleLineItemResponse> Items);

public record SaleListResponse(
    IReadOnlyList<SaleResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public record SaleReturnItemRequest(
    Guid ProductId,
    decimal Quantity);

public record CreateSaleReturnRequest(
    string Reason,
    string RefundMethod,
    List<SaleReturnItemRequest> Items);

public record SaleReturnItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    decimal SubTotal,
    decimal TotalCost);

public record SaleReturnResponse(
    Guid Id,
    Guid SaleId,
    string ReturnNumber,
    DateTimeOffset ReturnDate,
    decimal TotalAmount,
    decimal TotalCost,
    string RefundMethod,
    string Reason,
    IReadOnlyList<SaleReturnItemResponse> Items);
