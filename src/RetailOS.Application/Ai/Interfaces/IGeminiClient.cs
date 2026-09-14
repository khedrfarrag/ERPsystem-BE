namespace RetailOS.Application.Ai.Interfaces;

public record RawExtractedInvoice(
    string? SupplierName,
    string? InvoiceNumber,
    string? InvoiceDate,
    decimal TotalAmount,
    decimal? TaxAmount,
    decimal? DiscountAmount,
    IReadOnlyList<RawExtractedItem> Items
);

public record RawExtractedItem(
    string Name,
    string? Barcode,
    string? Category,
    string? Unit,
    decimal Quantity,
    decimal UnitCost,
    decimal Total
);

public interface IGeminiClient
{
    Task<RawExtractedInvoice> AnalyzeInvoiceImageAsync(
        byte[] imageBytes, 
        string mimeType, 
        CancellationToken cancellationToken = default);
}
