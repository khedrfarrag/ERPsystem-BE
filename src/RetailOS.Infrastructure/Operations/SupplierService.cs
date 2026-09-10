using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.Application.Suppliers.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreateSupplierRequest> _createValidator;
    private readonly IValidator<UpdateSupplierRequest> _updateValidator;
    private readonly IValidator<CreateRepresentativeRequest> _repValidator;

    public SupplierService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<CreateSupplierRequest> createValidator,
        IValidator<UpdateSupplierRequest> updateValidator,
        IValidator<CreateRepresentativeRequest> repValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _repValidator = repValidator;
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var nameNormalized = request.Name.Trim().ToLower();

        var exists = await _context.Suppliers.AnyAsync(s => s.Name.ToLower() == nameNormalized, cancellationToken);
        if (exists)
            throw new ConflictException("SUPPLIER_NAME_DUPLICATE", $"A supplier with name '{request.Name}' already exists.");

        var supplier = new Supplier
        {
            StoreId = storeId,
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true
        };

        _context.Suppliers.Add(supplier);

        if (request.OpeningBalance.HasValue && request.OpeningBalance.Value > 0)
        {
            var tx = new SupplierAccountTransaction
            {
                StoreId = storeId,
                SupplierId = supplier.Id,
                Type = SupplierTransactionType.OpeningBalance,
                Amount = request.OpeningBalance.Value,
                Notes = "Opening balance",
                CreatedBy = _userContext.CurrentUserId ?? Guid.Empty
            };
            _context.SupplierAccountTransactions.Add(tx);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(supplier.Id, cancellationToken)
            ?? throw new DomainException("INTERNAL_ERROR", "Failed to retrieve created supplier.");
    }

    public async Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.Representatives)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier is null)
            return null;

        var balance = await _context.SupplierAccountTransactions
            .AsNoTracking()
            .Where(t => t.SupplierId == id)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var reps = supplier.Representatives
            .Where(r => r.DeletedAt == null)
            .Select(r => new RepresentativeResponse(r.Id, r.SupplierId, r.Name, r.Phone, r.Notes, r.IsActive))
            .ToList();

        return new SupplierResponse(
            supplier.Id,
            supplier.Name,
            supplier.Phone,
            supplier.Address,
            supplier.Notes,
            balance,
            supplier.IsActive,
            reps);
    }

    public async Task<SupplierListResponse> GetAllAsync(
        string? search = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Suppliers.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Phone.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Representatives)
            .ToListAsync(cancellationToken);

        var supplierIds = suppliers.Select(s => s.Id).ToList();

        var balances = await _context.SupplierAccountTransactions
            .AsNoTracking()
            .Where(t => supplierIds.Contains(t.SupplierId))
            .GroupBy(t => t.SupplierId)
            .Select(g => new { SupplierId = g.Key, Balance = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Balance, cancellationToken);

        var items = suppliers.Select(s =>
        {
            var bal = balances.TryGetValue(s.Id, out var b) ? b : 0m;
            var reps = s.Representatives
                .Where(r => r.DeletedAt == null)
                .Select(r => new RepresentativeResponse(r.Id, r.SupplierId, r.Name, r.Phone, r.Notes, r.IsActive))
                .ToList();

            return new SupplierResponse(
                s.Id,
                s.Name,
                s.Phone,
                s.Address,
                s.Notes,
                bal,
                s.IsActive,
                reps);
        }).ToList();

        return new SupplierListResponse(items, total, pageNumber, pageSize);
    }

    public async Task<SupplierResponse?> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null)
            return null;

        var nameNormalized = request.Name.Trim().ToLower();
        var duplicate = await _context.Suppliers.AnyAsync(s => s.Id != id && s.Name.ToLower() == nameNormalized, cancellationToken);
        if (duplicate)
            throw new ConflictException("SUPPLIER_NAME_DUPLICATE", $"A supplier with name '{request.Name}' already exists.");

        supplier.Name = request.Name.Trim();
        supplier.Phone = request.Phone.Trim();
        supplier.Address = request.Address?.Trim();
        supplier.Notes = request.Notes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null)
            return false;

        var hasPurchases = await _context.Purchases.AnyAsync(p => p.SupplierId == id, cancellationToken);
        if (hasPurchases)
            throw new ConflictException("SUPPLIER_HAS_TRANSACTIONS", "Cannot delete supplier with linked purchases. Deactivate the supplier instead.");

        var hasTransactions = await _context.SupplierAccountTransactions
            .AnyAsync(t => t.SupplierId == id && t.Type != SupplierTransactionType.OpeningBalance, cancellationToken);
        if (hasTransactions)
            throw new ConflictException("SUPPLIER_HAS_TRANSACTIONS", "Cannot delete supplier with transaction history. Deactivate the supplier instead.");

        supplier.DeletedAt = DateTime.UtcNow;
        supplier.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RepresentativeResponse> AddRepresentativeAsync(Guid supplierId, CreateRepresentativeRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _repValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);
        if (supplier is null)
            throw new NotFoundException("SUPPLIER_NOT_FOUND", $"Supplier with ID '{supplierId}' was not found.");

        var rep = new SupplierRepresentative
        {
            StoreId = _storeContext.CurrentStoreId!.Value,
            SupplierId = supplierId,
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true
        };

        _context.SupplierRepresentatives.Add(rep);
        await _context.SaveChangesAsync(cancellationToken);

        return new RepresentativeResponse(rep.Id, rep.SupplierId, rep.Name, rep.Phone, rep.Notes, rep.IsActive);
    }

    public async Task<AccountStatementResponse?> GetStatementAsync(Guid supplierId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var supplier = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);
        if (supplier is null)
            return null;

        var query = _context.SupplierAccountTransactions
            .AsNoTracking()
            .Where(t => t.SupplierId == supplierId);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        var txs = await query.OrderBy(t => t.CreatedAt).ToListAsync(cancellationToken);

        decimal running = 0m;
        var items = new List<AccountStatementItemResponse>();

        foreach (var t in txs)
        {
            running += t.Amount;
            items.Add(new AccountStatementItemResponse(
                t.Id,
                t.CreatedAt,
                t.Type.ToString(),
                t.Amount,
                running,
                t.ReferenceId,
                t.Notes));
        }

        return new AccountStatementResponse(supplier.Id, supplier.Name, running, items);
    }
}
