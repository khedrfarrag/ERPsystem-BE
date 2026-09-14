using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Ai.DTOs;
using RetailOS.Application.Ai.Interfaces;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/v1/ai/invoices")]
[Route("api/ai/invoices")]
[Authorize]
public class AiInvoiceController : ControllerBase
{
    private readonly IAiInvoiceScannerService _scannerService;
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;

    public AiInvoiceController(
        IAiInvoiceScannerService scannerService,
        AppDbContext context,
        IStoreContext storeContext)
    {
        _scannerService = scannerService;
        _context = context;
        _storeContext = storeContext;
    }

    [HttpPost("scan")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceScanPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanInvoice(
        IFormFile file, 
        [FromQuery] decimal markupPercent = 25m, 
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new DomainException("INVALID_FILE", "يرجى اختيار صورة أو ملف فاتورة صالح للمسح الضوئي.", 400);
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new DomainException("FILE_SIZE_EXCEEDED", "حجم الملف يتجاوز الحد الأقصى (10 ميجابايت).", 400);
        }

        await using var stream = file.OpenReadStream();
        var result = await _scannerService.ScanAndExtractAsync(
            stream, 
            file.FileName, 
            markupPercent, 
            cancellationToken);

        return Ok(ApiResponse<InvoiceScanPreviewDto>.Ok(result));
    }

    [HttpPost("commit")]
    [ProducesResponseType(typeof(ApiResponse<CommitAiInvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CommitInvoice(
        [FromBody] CommitAiInvoiceRequest request, 
        CancellationToken cancellationToken = default)
    {
        var result = await _scannerService.CommitInvoiceAsync(request, cancellationToken);
        return Ok(ApiResponse<CommitAiInvoiceResponse>.Ok(result));
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<AiInvoiceSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
        {
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");
        }

        var store = await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == _storeContext.CurrentStoreId!.Value, cancellationToken);

        var enableArchiving = store?.EnableInvoiceArchiving ?? true;

        return Ok(ApiResponse<AiInvoiceSettingsDto>.Ok(new AiInvoiceSettingsDto(
            EnableInvoiceArchiving: enableArchiving,
            DefaultMarkupPercent: 25.0m
        )));
    }
}

public record AiInvoiceSettingsDto(
    bool EnableInvoiceArchiving,
    decimal DefaultMarkupPercent
);
