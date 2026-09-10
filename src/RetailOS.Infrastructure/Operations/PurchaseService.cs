using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Purchases.DTOs;
using RetailOS.Application.Purchases.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreatePurchaseRequest> _createValidator;
    private readonly IValidator<CreatePurchaseReturnRequest> _returnValidator;

    public PurchaseService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<CreatePurchaseRequest> createValidator,
        IValidator<CreatePurchaseReturnRequest> returnValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _createValidator = createValidator;
        _returnValidator = returnValidator;
    }

    public async Task<PurchaseResponse> CreateDraftAsync(CreatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (supplier is null || !supplier.IsActive)
            throw new DomainException("SUPPLIER_INACTIVE", "Supplier does not exist or is inactive.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count || products.Any(p => !p.IsActive))
            throw new DomainException("PRODUCT_INACTIVE", "One or more products do not exist or are inactive.");

        var purchaseNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];

        var purchase = new Purchase
        {
            StoreId = storeId,
            SupplierId = request.SupplierId,
            PurchaseNumber = purchaseNumber,
            InvoiceNumber = request.InvoiceNumber?.Trim(),
            PurchaseDate = request.PurchaseDate,
            Status = PurchaseStatus.Draft,
            Notes = request.Notes?.Trim(),
            CreatedBy = _userContext.CurrentUserId ?? Guid.Empty
        };

        decimal total = 0m;
        foreach (var item in request.Items)
        {
            var subTotal = Math.Max(0m, (item.Quantity * item.UnitCost) - item.Discount);
            total += subTotal;

            purchase.LineItems.Add(new PurchaseLineItem
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                Discount = item.Discount,
                SubTotal = subTotal
            });
        }

        purchase.TotalAmount = total;
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(purchase.Id, cancellationToken)
            ?? throw new DomainException("INTERNAL_ERROR", "Failed to retrieve created purchase.");
    }

    public async Task<PurchaseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var purchase = await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.LineItems)
                .ThenInclude(li => li.Product)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (purchase is null)
            return null;

        var items = purchase.LineItems.Select(li => new PurchaseLineItemResponse(
            li.Id,
            li.ProductId,
            li.Product.Name,
            li.Quantity,
            li.UnitCost,
            li.Discount,
            li.SubTotal)).ToList();

        return new PurchaseResponse(
            purchase.Id,
            purchase.SupplierId,
            purchase.Supplier.Name,
            purchase.PurchaseNumber,
            purchase.InvoiceNumber,
            purchase.PurchaseDate,
            purchase.Status.ToString(),
            purchase.TotalAmount,
            purchase.Notes,
            purchase.CreatedAt,
            items);
    }

    public async Task<PurchaseListResponse> GetAllAsync(
        Guid? supplierId = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Purchases.AsNoTracking().Include(p => p.Supplier).AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseStatus>(status, true, out var pStatus))
            query = query.Where(p => p.Status == pStatus);

        var total = await query.CountAsync(cancellationToken);

        var purchases = await query
            .OrderByDescending(p => p.PurchaseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.LineItems)
                .ThenInclude(li => li.Product)
            .ToListAsync(cancellationToken);

        var items = purchases.Select(p =>
        {
            var lineResponses = p.LineItems.Select(li => new PurchaseLineItemResponse(
                li.Id,
                li.ProductId,
                li.Product.Name,
                li.Quantity,
                li.UnitCost,
                li.Discount,
                li.SubTotal)).ToList();

            return new PurchaseResponse(
                p.Id,
                p.SupplierId,
                p.Supplier.Name,
                p.PurchaseNumber,
                p.InvoiceNumber,
                p.PurchaseDate,
                p.Status.ToString(),
                p.TotalAmount,
                p.Notes,
                p.CreatedAt,
                lineResponses);
        }).ToList();

        return new PurchaseListResponse(items, total, pageNumber, pageSize);
    }

    public async Task<PurchaseResponse?> UpdateDraftAsync(Guid id, UpdatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var purchase = await _context.Purchases
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (purchase is null)
            return null;

        if (purchase.Status != PurchaseStatus.Draft)
            throw new ConflictException("PURCHASE_ALREADY_CONFIRMED", "Only draft purchases can be edited.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count || products.Any(p => !p.IsActive))
            throw new DomainException("PRODUCT_INACTIVE", "One or more products do not exist or are inactive.");

        purchase.InvoiceNumber = request.InvoiceNumber?.Trim();
        purchase.PurchaseDate = request.PurchaseDate;
        purchase.Notes = request.Notes?.Trim();

        foreach (var existing in purchase.LineItems.ToList())
        {
            _context.PurchaseLineItems.Remove(existing);
        }

        decimal total = 0m;
        foreach (var item in request.Items)
        {
            var subTotal = Math.Max(0m, (item.Quantity * item.UnitCost) - item.Discount);
            total += subTotal;

            _context.PurchaseLineItems.Add(new PurchaseLineItem
            {
                StoreId = _storeContext.CurrentStoreId!.Value,
                PurchaseId = purchase.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                Discount = item.Discount,
                SubTotal = subTotal
            });
        }

        purchase.TotalAmount = total;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<PurchaseResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var storeId = _storeContext.CurrentStoreId!.Value;

        var purchase = await _context.Purchases
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (purchase is null)
            throw new NotFoundException("PURCHASE_NOT_FOUND", "Purchase not found.");

        if (purchase.Status == PurchaseStatus.Confirmed)
            throw new ConflictException("PURCHASE_ALREADY_CONFIRMED", "Purchase is already confirmed.");

        var productIds = purchase.LineItems.Select(li => li.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var currentStocks = await _context.InventoryTransactions
            .Where(it => productIds.Contains(it.ProductId))
            .GroupBy(it => it.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        foreach (var item in purchase.LineItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                throw new DomainException("PRODUCT_INACTIVE", $"Product '{item.ProductId}' is inactive or was not found.");

            var currentStock = currentStocks.TryGetValue(item.ProductId, out var s) ? s : 0m;
            var currentCost = product.PurchaseCost ?? item.UnitCost;

            // WAC calculation
            decimal newWac;
            if (currentStock <= 0m)
            {
                newWac = item.UnitCost;
            }
            else
            {
                var totalCost = (currentStock * currentCost) + (item.Quantity * item.UnitCost);
                var totalQty = currentStock + item.Quantity;
                newWac = Math.Round(totalCost / totalQty, 6, MidpointRounding.AwayFromZero);
            }

            product.PurchaseCost = newWac;

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                CostPerUnit = item.UnitCost,
                Reason = InventoryTransactionReason.Purchase,
                ReferenceId = purchase.Id,
                Notes = $"Purchase {purchase.PurchaseNumber}",
                CreatedBy = userId
            });
        }

        // Supplier ledger transaction
        _context.SupplierAccountTransactions.Add(new SupplierAccountTransaction
        {
            StoreId = storeId,
            SupplierId = purchase.SupplierId,
            Type = SupplierTransactionType.Purchase,
            Amount = purchase.TotalAmount,
            ReferenceId = purchase.Id,
            Notes = $"Invoice {purchase.PurchaseNumber}",
            CreatedBy = userId
        });

        purchase.Status = PurchaseStatus.Confirmed;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("INTERNAL_ERROR", "Failed to retrieve confirmed purchase.");
    }

    public async Task<PurchaseReturnResponse> CreateReturnAsync(Guid purchaseId, CreatePurchaseReturnRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _returnValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;

        var purchase = await _context.Purchases
            .Include(p => p.LineItems)
            .Include(p => p.Returns)
                .ThenInclude(r => r.LineItems)
            .FirstOrDefaultAsync(p => p.Id == purchaseId, cancellationToken);

        if (purchase is null)
            throw new NotFoundException("PURCHASE_NOT_FOUND", "Purchase not found.");

        if (purchase.Status != PurchaseStatus.Confirmed)
            throw new ConflictException("PURCHASE_NOT_CONFIRMED", "Returns can only be processed against confirmed purchases.");

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);
        var allowNegative = store?.AllowNegativeStock ?? false;

        var returnNumber = $"PR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
        var pReturn = new PurchaseReturn
        {
            StoreId = storeId,
            PurchaseId = purchaseId,
            ReturnNumber = returnNumber,
            ReturnDate = DateTimeOffset.UtcNow,
            Reason = request.Reason.Trim(),
            CreatedBy = _userContext.CurrentUserId ?? Guid.Empty
        };

        decimal totalReturn = 0m;
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        foreach (var returnItem in request.Items)
        {
            var origLine = purchase.LineItems.FirstOrDefault(li => li.ProductId == returnItem.ProductId);
            if (origLine is null)
                throw new DomainException("INVALID_RETURN_ITEM", $"Product '{returnItem.ProductId}' was not part of this purchase.");

            var previouslyReturned = purchase.Returns
                .SelectMany(r => r.LineItems)
                .Where(li => li.ProductId == returnItem.ProductId)
                .Sum(li => li.Quantity);

            if (previouslyReturned + returnItem.Quantity > origLine.Quantity)
                throw new DomainException("EXCESS_RETURN_QUANTITY", $"Cannot return more units ({returnItem.Quantity}) than purchased remaining ({origLine.Quantity - previouslyReturned}).");

            if (!allowNegative)
            {
                var currentStock = await _context.InventoryTransactions
                    .Where(it => it.ProductId == returnItem.ProductId)
                    .SumAsync(it => (decimal?)it.Quantity, cancellationToken) ?? 0m;

                if (currentStock < returnItem.Quantity)
                    throw new DomainException("INSUFFICIENT_STOCK", $"Cannot return {returnItem.Quantity} units; current available stock is {currentStock}.");
            }

            var subTotal = returnItem.Quantity * origLine.UnitCost;
            totalReturn += subTotal;

            pReturn.LineItems.Add(new PurchaseReturnLineItem
            {
                StoreId = storeId,
                ProductId = returnItem.ProductId,
                Quantity = returnItem.Quantity,
                UnitCost = origLine.UnitCost,
                SubTotal = subTotal
            });

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                StoreId = storeId,
                ProductId = returnItem.ProductId,
                Quantity = -returnItem.Quantity,
                CostPerUnit = origLine.UnitCost,
                Reason = InventoryTransactionReason.PurchaseReturn,
                ReferenceId = purchase.Id,
                Notes = $"Return {returnNumber}",
                CreatedBy = userId
            });
        }

        pReturn.TotalAmount = totalReturn;
        _context.PurchaseReturns.Add(pReturn);

        // Reduce supplier payable
        _context.SupplierAccountTransactions.Add(new SupplierAccountTransaction
        {
            StoreId = storeId,
            SupplierId = purchase.SupplierId,
            Type = SupplierTransactionType.PurchaseReturn,
            Amount = -totalReturn,
            ReferenceId = pReturn.Id,
            Notes = $"Return {returnNumber}",
            CreatedBy = userId
        });

        await _context.SaveChangesAsync(cancellationToken);

        var products = await _context.Products
            .Where(p => request.Items.Select(i => i.ProductId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var lineResponses = pReturn.LineItems.Select(li => new PurchaseReturnItemResponse(
            li.Id,
            li.ProductId,
            products.TryGetValue(li.ProductId, out var n) ? n : "",
            li.Quantity,
            li.UnitCost,
            li.SubTotal)).ToList();

        return new PurchaseReturnResponse(
            pReturn.Id,
            pReturn.PurchaseId,
            pReturn.ReturnNumber,
            pReturn.ReturnDate,
            pReturn.TotalAmount,
            pReturn.Reason,
            lineResponses);
    }
}
