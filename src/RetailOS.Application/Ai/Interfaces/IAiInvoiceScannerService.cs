using RetailOS.Application.Ai.DTOs;

namespace RetailOS.Application.Ai.Interfaces;

public interface IAiInvoiceScannerService
{
    Task<InvoiceScanPreviewDto> ScanAndExtractAsync(
        Stream fileStream, 
        string fileName, 
        decimal defaultMarkupPercent = 25m, 
        CancellationToken cancellationToken = default);

    Task<CommitAiInvoiceResponse> CommitInvoiceAsync(
        CommitAiInvoiceRequest request, 
        CancellationToken cancellationToken = default);
}
