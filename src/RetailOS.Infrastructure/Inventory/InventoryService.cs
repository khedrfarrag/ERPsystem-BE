using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Inventory.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Inventory;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<RecordOpeningStockRequest> _validator;

    public InventoryService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<RecordOpeningStockRequest> validator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _validator = validator;
    }

    public async Task<OpeningStockResponse> RecordOpeningStockAsync(
        RecordOpeningStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
            throw new NotFoundException("PRODUCT_NOT_FOUND", "The specified product was not found.");

        if (!product.IsActive)
            throw new DomainException("PRODUCT_INACTIVE", "Cannot record opening stock for an inactive product.", 400);

        var exists = await _context.InventoryTransactions
            .AnyAsync(t => t.ProductId == request.ProductId && t.Reason == InventoryTransactionReason.OpeningBalance, cancellationToken);

        if (exists)
            throw new ConflictException("OPENING_STOCK_ALREADY_EXISTS", "Opening stock has already been recorded for this product.");

        var userId = _userContext.CurrentUserId ?? throw new UnauthorizedException("UNAUTHORIZED", "User identity not found.");

        var tx = new InventoryTransaction
        {
            StoreId = _storeContext.CurrentStoreId!.Value,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            CostPerUnit = request.CostPerUnit,
            Reason = InventoryTransactionReason.OpeningBalance,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.InventoryTransactions.Add(tx);
        product.PurchaseCost = request.CostPerUnit;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("OPENING_STOCK_ALREADY_EXISTS", "Opening stock has already been recorded for this product.");
        }

        return new OpeningStockResponse(
            tx.Id,
            product.Id,
            product.Name,
            tx.Quantity,
            tx.CostPerUnit,
            tx.Quantity * tx.CostPerUnit,
            tx.Reason.ToString(),
            tx.CreatedAt
        );
    }

    public async Task<BulkOpeningStockResponse> BulkRecordOpeningStockAsync(
        BulkOpeningStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var userId = _userContext.CurrentUserId ?? throw new UnauthorizedException("UNAUTHORIZED", "User identity not found.");

        var storeProducts = await _context.Products
            .Where(p => p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var existingOpeningStocks = (await _context.InventoryTransactions
            .Where(t => t.Reason == InventoryTransactionReason.OpeningBalance)
            .Select(t => t.ProductId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var created = 0;
        var skippedAlreadyExists = 0;
        var skippedInvalid = 0;
        var errors = new List<BulkOpeningStockEntryError>();
        var batchToCreate = new List<InventoryTransaction>();

        foreach (var entry in request.Entries)
        {
            if (entry.Quantity <= 0 || entry.CostPerUnit < 0)
            {
                skippedInvalid++;
                errors.Add(new BulkOpeningStockEntryError(entry.ProductId, "INVALID_VALUES", "Quantity must be > 0 and cost >= 0."));
                continue;
            }

            if (!storeProducts.TryGetValue(entry.ProductId, out var product))
            {
                skippedInvalid++;
                errors.Add(new BulkOpeningStockEntryError(entry.ProductId, "PRODUCT_NOT_FOUND", "Product was not found or is inactive."));
                continue;
            }

            if (existingOpeningStocks.Contains(entry.ProductId))
            {
                skippedAlreadyExists++;
                errors.Add(new BulkOpeningStockEntryError(entry.ProductId, "OPENING_STOCK_ALREADY_EXISTS", "Opening stock has already been recorded for this product."));
                continue;
            }

            var tx = new InventoryTransaction
            {
                StoreId = _storeContext.CurrentStoreId!.Value,
                ProductId = entry.ProductId,
                Quantity = entry.Quantity,
                CostPerUnit = entry.CostPerUnit,
                Reason = InventoryTransactionReason.OpeningBalance,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            batchToCreate.Add(tx);
            existingOpeningStocks.Add(entry.ProductId);
            created++;
        }

        if (batchToCreate.Count > 0)
        {
            _context.InventoryTransactions.AddRange(batchToCreate);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new BulkOpeningStockResponse(
            TotalRequested: request.Entries.Count,
            Created: created,
            SkippedAlreadyExists: skippedAlreadyExists,
            SkippedInvalid: skippedInvalid,
            Errors: errors
        );
    }

    public async Task<OpeningStockListResponse> GetOpeningStockEntriesAsync(
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => t.Reason == InventoryTransactionReason.OpeningBalance);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new OpeningStockResponse(
                t.Id,
                t.ProductId,
                t.Product.Name,
                t.Quantity,
                t.CostPerUnit,
                t.Quantity * t.CostPerUnit,
                t.Reason.ToString(),
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new OpeningStockListResponse(items, totalCount, page, pageSize);
    }
}
