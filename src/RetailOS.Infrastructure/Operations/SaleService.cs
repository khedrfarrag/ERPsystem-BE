using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Sales.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class SaleService : ISaleService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreateSaleRequest> _createValidator;
    private readonly IValidator<CreateSaleReturnRequest> _returnValidator;

    public SaleService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<CreateSaleRequest> createValidator,
        IValidator<CreateSaleReturnRequest> returnValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _createValidator = createValidator;
        _returnValidator = returnValidator;
    }

    public async Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var pMethod = Enum.Parse<SalePaymentMethod>(request.PaymentMethod, true);

        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value, cancellationToken);
            if (customer is null || !customer.IsActive)
                throw new DomainException("CUSTOMER_INACTIVE", "Customer does not exist or is inactive.");
        }

        var distinctProductIds = request.Items.Select(i => i.ProductId).Distinct().OrderBy(id => id).ToList();

        // 1. Transaction with Pessimistic Row Locking on products (in sorted order to prevent deadlocks)
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        await _context.Database.ExecuteSqlRawAsync(
            "SELECT id FROM products WHERE id = ANY({0}) AND store_id = {1} FOR UPDATE",
            new object[] { distinctProductIds.ToArray(), storeId },
            cancellationToken);

        var products = await _context.Products
            .Where(p => distinctProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        if (products.Count != distinctProductIds.Count || products.Values.Any(p => !p.IsActive))
            throw new DomainException("PRODUCT_INACTIVE", "One or more products do not exist or are inactive.");

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);
        var allowNegative = store?.AllowNegativeStock ?? false;

        // Group requested quantities per product
        var requestedQuantities = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var currentStocks = await _context.InventoryTransactions
            .Where(it => distinctProductIds.Contains(it.ProductId))
            .GroupBy(it => it.ProductId)
            .Select(g => new { ProductId = g.Key, Available = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Available, cancellationToken);

        // Enforce stock guard: Per Clarification Q1, reject ENTIRE sale with per-line breakdown
        if (!allowNegative)
        {
            var shortages = new List<StockShortageItem>();
            foreach (var kvp in requestedQuantities)
            {
                var prodId = kvp.Key;
                var requestedQty = kvp.Value;
                var availableQty = currentStocks.TryGetValue(prodId, out var s) ? s : 0m;

                if (availableQty < requestedQty)
                {
                    var prodName = products[prodId].Name;
                    shortages.Add(new StockShortageItem(prodId, prodName, requestedQty, availableQty));
                }
            }

            if (shortages.Count > 0)
            {
                var errorMessages = shortages
                    .Select(s => $"Product '{s.ProductName}' requested: {s.RequestedQuantity}, available: {s.AvailableQuantity}")
                    .ToList();

                throw new DomainException(
                    "INSUFFICIENT_STOCK",
                    "One or more products have insufficient stock to complete the sale.",
                    400,
                    errorMessages);
            }
        }

        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        var sale = new Sale
        {
            StoreId = storeId,
            CustomerId = request.CustomerId,
            InvoiceNumber = invoiceNumber,
            SaleDate = DateTimeOffset.UtcNow,
            Status = SaleStatus.Completed,
            PaymentMethod = pMethod,
            Notes = request.Notes?.Trim(),
            CreatedBy = userId
        };

        decimal subTotal = 0m;
        decimal totalCost = 0m;

        var productCosts = products.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.PurchaseCost ?? 0m);

        var missingCostIds = productCosts.Where(kvp => kvp.Value <= 0m).Select(kvp => kvp.Key).ToList();
        if (missingCostIds.Count > 0)
        {
            var latestCosts = await _context.InventoryTransactions
                .Where(it => missingCostIds.Contains(it.ProductId) && it.CostPerUnit > 0m)
                .GroupBy(it => it.ProductId)
                .Select(g => new { ProductId = g.Key, Cost = g.OrderByDescending(x => x.CreatedAt).Select(x => x.CostPerUnit).FirstOrDefault() })
                .ToDictionaryAsync(x => x.ProductId, x => x.Cost, cancellationToken);

            foreach (var kvp in latestCosts)
            {
                productCosts[kvp.Key] = kvp.Value;
            }
        }

        foreach (var item in request.Items)
        {
            var prod = products[item.ProductId];
            var unitCost = productCosts.TryGetValue(item.ProductId, out var c) ? c : 0m;
            var lineSubTotal = Math.Max(0m, (item.Quantity * item.UnitPrice) - item.Discount);
            var lineTotalCost = item.Quantity * unitCost;

            subTotal += lineSubTotal;
            totalCost += lineTotalCost;

            sale.LineItems.Add(new SaleLineItem
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UnitCost = unitCost,
                Discount = item.Discount,
                SubTotal = lineSubTotal,
                TotalCost = lineTotalCost
            });

            // Inventory decrement
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Quantity = -item.Quantity,
                CostPerUnit = unitCost,
                Reason = InventoryTransactionReason.Sale,
                ReferenceId = sale.Id,
                Notes = $"Sale {invoiceNumber}",
                CreatedBy = userId
            });
        }

        sale.SubTotal = subTotal;
        sale.TotalAmount = subTotal;
        sale.TotalCost = totalCost;

        if (pMethod == SalePaymentMethod.Cash)
        {
            sale.CashAmount = subTotal;
            sale.CreditAmount = 0m;
        }
        else if (pMethod == SalePaymentMethod.Credit)
        {
            sale.CashAmount = 0m;
            sale.CreditAmount = subTotal;
        }
        else // Mixed
        {
            sale.CashAmount = request.CashAmount;
            sale.CreditAmount = Math.Max(0m, subTotal - request.CashAmount);
        }

        _context.Sales.Add(sale);

        // Ledger postings
        if (sale.CreditAmount > 0m && request.CustomerId.HasValue)
        {
            _context.CustomerAccountTransactions.Add(new CustomerAccountTransaction
            {
                StoreId = storeId,
                CustomerId = request.CustomerId.Value,
                Type = CustomerTransactionType.Sale,
                Amount = sale.CreditAmount,
                ReferenceId = sale.Id,
                Notes = $"Sale {invoiceNumber}",
                CreatedBy = userId
            });
        }

        if (sale.CashAmount > 0m)
        {
            _context.CashRegisterTransactions.Add(new CashRegisterTransaction
            {
                StoreId = storeId,
                Type = CashTransactionType.CashSale,
                Amount = sale.CashAmount,
                ReferenceId = sale.Id,
                Notes = $"Cash Sale {invoiceNumber}",
                CreatedBy = userId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return await GetByIdAsync(sale.Id, cancellationToken)
            ?? throw new DomainException("INTERNAL_ERROR", "Failed to retrieve completed sale.");
    }

    public async Task<SaleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var sale = await _context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.LineItems)
                .ThenInclude(li => li.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sale is null)
            return null;

        var lineResponses = sale.LineItems.Select(li => new SaleLineItemResponse(
            li.Id,
            li.ProductId,
            li.Product.Name,
            li.Quantity,
            li.UnitPrice,
            li.UnitCost,
            li.Discount,
            li.SubTotal,
            li.TotalCost)).ToList();

        return new SaleResponse(
            sale.Id,
            sale.CustomerId,
            sale.Customer?.Name,
            sale.InvoiceNumber,
            sale.SaleDate,
            sale.Status.ToString(),
            sale.PaymentMethod.ToString(),
            sale.SubTotal,
            sale.DiscountAmount,
            sale.TaxAmount,
            sale.TotalAmount,
            sale.CashAmount,
            sale.CreditAmount,
            sale.TotalCost,
            sale.Notes,
            sale.CreatedAt,
            lineResponses);
    }

    public async Task<SaleListResponse> GetAllAsync(
        Guid? customerId = null,
        string? paymentMethod = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Sales.AsNoTracking().Include(s => s.Customer).AsQueryable();

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(paymentMethod) && Enum.TryParse<SalePaymentMethod>(paymentMethod, true, out var pm))
            query = query.Where(s => s.PaymentMethod == pm);

        var total = await query.CountAsync(cancellationToken);

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.LineItems)
                .ThenInclude(li => li.Product)
            .ToListAsync(cancellationToken);

        var items = sales.Select(s =>
        {
            var lineResponses = s.LineItems.Select(li => new SaleLineItemResponse(
                li.Id,
                li.ProductId,
                li.Product.Name,
                li.Quantity,
                li.UnitPrice,
                li.UnitCost,
                li.Discount,
                li.SubTotal,
                li.TotalCost)).ToList();

            return new SaleResponse(
                s.Id,
                s.CustomerId,
                s.Customer?.Name,
                s.InvoiceNumber,
                s.SaleDate,
                s.Status.ToString(),
                s.PaymentMethod.ToString(),
                s.SubTotal,
                s.DiscountAmount,
                s.TaxAmount,
                s.TotalAmount,
                s.CashAmount,
                s.CreditAmount,
                s.TotalCost,
                s.Notes,
                s.CreatedAt,
                lineResponses);
        }).ToList();

        return new SaleListResponse(items, total, pageNumber, pageSize);
    }

    public async Task<SaleReturnResponse> CreateReturnAsync(Guid saleId, CreateSaleReturnRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _returnValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var rMethod = Enum.Parse<RefundMethod>(request.RefundMethod, true);

        var sale = await _context.Sales
            .Include(s => s.LineItems)
            .Include(s => s.Returns)
                .ThenInclude(r => r.LineItems)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken);

        if (sale is null)
            throw new NotFoundException("SALE_NOT_FOUND", "Sale not found.");

        if (sale.Status != SaleStatus.Completed)
            throw new ConflictException("SALE_NOT_COMPLETED", "Only completed sales can be returned.");

        var returnNumber = $"SR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        var sReturn = new SaleReturn
        {
            StoreId = storeId,
            SaleId = saleId,
            ReturnNumber = returnNumber,
            ReturnDate = DateTimeOffset.UtcNow,
            RefundMethod = rMethod,
            Reason = request.Reason.Trim(),
            CreatedBy = userId
        };

        decimal totalAmount = 0m;
        decimal totalCost = 0m;

        foreach (var item in request.Items)
        {
            var origLine = sale.LineItems.FirstOrDefault(li => li.ProductId == item.ProductId);
            if (origLine is null)
                throw new DomainException("INVALID_RETURN_ITEM", $"Product '{item.ProductId}' was not part of this sale.");

            var previouslyReturned = sale.Returns
                .SelectMany(r => r.LineItems)
                .Where(li => li.ProductId == item.ProductId)
                .Sum(li => li.Quantity);

            if (previouslyReturned + item.Quantity > origLine.Quantity)
                throw new DomainException("EXCESS_RETURN_QUANTITY", $"Cannot return more units ({item.Quantity}) than purchased remaining ({origLine.Quantity - previouslyReturned}).");

            var lineSubTotal = item.Quantity * origLine.UnitPrice;
            // Restock at original unit cost per Clarification Q2
            var lineCostReversal = item.Quantity * origLine.UnitCost;

            totalAmount += lineSubTotal;
            totalCost += lineCostReversal;

            sReturn.LineItems.Add(new SaleReturnLineItem
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = origLine.UnitPrice,
                UnitCost = origLine.UnitCost,
                SubTotal = lineSubTotal,
                TotalCost = lineCostReversal
            });

            // Restock inventory with SaleReturn reason
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                CostPerUnit = origLine.UnitCost,
                Reason = InventoryTransactionReason.SaleReturn,
                ReferenceId = sale.Id,
                Notes = $"Sale Return {returnNumber}",
                CreatedBy = userId
            });
        }

        sReturn.TotalAmount = totalAmount;
        sReturn.TotalCost = totalCost;
        _context.SaleReturns.Add(sReturn);

        // Balance / Register adjustments
        if (rMethod == RefundMethod.Credit && sale.CustomerId.HasValue)
        {
            _context.CustomerAccountTransactions.Add(new CustomerAccountTransaction
            {
                StoreId = storeId,
                CustomerId = sale.CustomerId.Value,
                Type = CustomerTransactionType.SaleReturn,
                Amount = -totalAmount,
                ReferenceId = sReturn.Id,
                Notes = $"Return {returnNumber}",
                CreatedBy = userId
            });
        }
        else if (rMethod == RefundMethod.Cash)
        {
            _context.CashRegisterTransactions.Add(new CashRegisterTransaction
            {
                StoreId = storeId,
                Type = CashTransactionType.CashPaymentOut,
                Amount = -totalAmount,
                ReferenceId = sReturn.Id,
                Notes = $"Refund {returnNumber}",
                CreatedBy = userId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        var products = await _context.Products
            .Where(p => request.Items.Select(i => i.ProductId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var lineResponses = sReturn.LineItems.Select(li => new SaleReturnItemResponse(
            li.Id,
            li.ProductId,
            products.TryGetValue(li.ProductId, out var n) ? n : "",
            li.Quantity,
            li.UnitPrice,
            li.UnitCost,
            li.SubTotal,
            li.TotalCost)).ToList();

        return new SaleReturnResponse(
            sReturn.Id,
            sReturn.SaleId,
            sReturn.ReturnNumber,
            sReturn.ReturnDate,
            sReturn.TotalAmount,
            sReturn.TotalCost,
            sReturn.RefundMethod.ToString(),
            sReturn.Reason,
            lineResponses);
    }
}
