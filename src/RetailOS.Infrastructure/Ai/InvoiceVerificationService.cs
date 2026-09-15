using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Ai.DTOs;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Ai;

public interface IInvoiceVerificationService
{
    Task<DuplicateInvoiceWarningDto?> CheckDuplicateAsync(
        Guid storeId, 
        Guid? supplierId, 
        string? invoiceNumber, 
        decimal totalAmount, 
        IReadOnlyList<InvoiceScanLineItemDto> scannedItems, 
        CancellationToken cancellationToken = default);

    Task<DuplicateInvoiceWarningDto?> CheckDuplicateAsync(
        Guid storeId, 
        Guid? supplierId, 
        string? invoiceNumber, 
        decimal totalAmount, 
        IReadOnlyList<CommitAiInvoiceItemDto> commitItems, 
        CancellationToken cancellationToken = default);
}

public class InvoiceVerificationService : IInvoiceVerificationService
{
    private readonly AppDbContext _context;

    public InvoiceVerificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DuplicateInvoiceWarningDto?> CheckDuplicateAsync(
        Guid storeId, 
        Guid? supplierId, 
        string? invoiceNumber, 
        decimal totalAmount, 
        IReadOnlyList<InvoiceScanLineItemDto> scannedItems, 
        CancellationToken cancellationToken = default)
    {
        if (!supplierId.HasValue || string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return null;
        }

        var cleanInvoiceNum = invoiceNumber.Trim();

        // 1. Verification Logic: Check database for any existing un-deleted invoice with same InvoiceNumber & SupplierId
        var existingPurchase = await _context.Purchases
            .AsNoTracking()
            .Include(p => p.LineItems)
            .Where(p => p.StoreId == storeId 
                         
                        && p.SupplierId == supplierId.Value 
                        && p.InvoiceNumber != null 
                        && p.InvoiceNumber.Trim().ToLower() == cleanInvoiceNum.ToLower())
            .OrderByDescending(p => p.PurchaseDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPurchase == null)
        {
            return null;
        }

        // 2. Guardrails & Edge Cases: Check if 100% identical
        bool isIdentical = false;
        if (Math.Abs(existingPurchase.TotalAmount - totalAmount) < 0.01m &&
            existingPurchase.LineItems.Count == scannedItems.Count)
        {
            // Compare line quantities & costs
            var existingItemsSummary = existingPurchase.LineItems
                .Select(li => (li.Quantity, Math.Round(li.UnitCost, 2)))
                .OrderBy(x => x.Quantity).ThenBy(x => x.Item2)
                .ToList();

            var scannedSummary = scannedItems
                .Select(si => (si.Quantity, Math.Round(si.UnitCost, 2)))
                .OrderBy(x => x.Quantity).ThenBy(x => x.Item2)
                .ToList();

            isIdentical = existingItemsSummary.SequenceEqual(scannedSummary);
        }

        var warningMessage = isIdentical
            ? $"تحذير أمني: تم العثور على فاتورة سابقة متطابقة تماماً برقم ({existingPurchase.InvoiceNumber}) وتاريخ {existingPurchase.PurchaseDate:yyyy-MM-dd} بإجمالي {existingPurchase.TotalAmount:N2}. يتطلب تكرار حفظ هذه الفاتورة صلاحية المشرف."
            : $"تنبيه: تم العثور على فاتورة سابقة بنفس الرقم ({existingPurchase.InvoiceNumber}) للمورد بتفاصيل مختلفة مسجلة بتاريخ {existingPurchase.PurchaseDate:yyyy-MM-dd} وإجمالي {existingPurchase.TotalAmount:N2}. هل ترغب في المتابعة؟";

        return new DuplicateInvoiceWarningDto(
            IsDuplicate: true,
            ExistingPurchaseId: existingPurchase.Id,
            ExistingPurchaseNumber: existingPurchase.PurchaseNumber,
            ExistingPurchaseDate: existingPurchase.PurchaseDate,
            ExistingTotalAmount: existingPurchase.TotalAmount,
            IsIdentical: isIdentical,
            WarningMessage: warningMessage
        );
    }

    public async Task<DuplicateInvoiceWarningDto?> CheckDuplicateAsync(
        Guid storeId, 
        Guid? supplierId, 
        string? invoiceNumber, 
        decimal totalAmount, 
        IReadOnlyList<CommitAiInvoiceItemDto> commitItems, 
        CancellationToken cancellationToken = default)
    {
        if (!supplierId.HasValue || string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return null;
        }

        var cleanInvoiceNum = invoiceNumber.Trim();

        var existingPurchase = await _context.Purchases
            .AsNoTracking()
            .Include(p => p.LineItems)
            .Where(p => p.StoreId == storeId 
                         
                        && p.SupplierId == supplierId.Value 
                        && p.InvoiceNumber != null 
                        && p.InvoiceNumber.Trim().ToLower() == cleanInvoiceNum.ToLower())
            .OrderByDescending(p => p.PurchaseDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPurchase == null)
        {
            return null;
        }

        bool isIdentical = false;
        if (Math.Abs(existingPurchase.TotalAmount - totalAmount) < 0.01m &&
            existingPurchase.LineItems.Count == commitItems.Count)
        {
            var existingItemsSummary = existingPurchase.LineItems
                .Select(li => (li.Quantity, Math.Round(li.UnitCost, 2)))
                .OrderBy(x => x.Quantity).ThenBy(x => x.Item2)
                .ToList();

            var commitSummary = commitItems
                .Select(ci => (ci.Quantity, Math.Round(ci.UnitCost, 2)))
                .OrderBy(x => x.Quantity).ThenBy(x => x.Item2)
                .ToList();

            isIdentical = existingItemsSummary.SequenceEqual(commitSummary);
        }

        var warningMessage = isIdentical
            ? $"تحذير أمني: تم العثور على فاتورة سابقة متطابقة تماماً برقم ({existingPurchase.InvoiceNumber}) وتاريخ {existingPurchase.PurchaseDate:yyyy-MM-dd} بإجمالي {existingPurchase.TotalAmount:N2}. يتطلب تكرار حفظ هذه الفاتورة صلاحية المشرف."
            : $"تنبيه: تم العثور على فاتورة سابقة بنفس الرقم ({existingPurchase.InvoiceNumber}) للمورد بتفاصيل مختلفة مسجلة بتاريخ {existingPurchase.PurchaseDate:yyyy-MM-dd} وإجمالي {existingPurchase.TotalAmount:N2}. هل ترغب في المتابعة؟";

        return new DuplicateInvoiceWarningDto(
            IsDuplicate: true,
            ExistingPurchaseId: existingPurchase.Id,
            ExistingPurchaseNumber: existingPurchase.PurchaseNumber,
            ExistingPurchaseDate: existingPurchase.PurchaseDate,
            ExistingTotalAmount: existingPurchase.TotalAmount,
            IsIdentical: isIdentical,
            WarningMessage: warningMessage
        );
    }
}
