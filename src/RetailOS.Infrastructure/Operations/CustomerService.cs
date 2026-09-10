using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Customers.Interfaces;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;

    public CustomerService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var nameNormalized = request.Name.Trim().ToLower();

        var exists = await _context.Customers.AnyAsync(c => c.Name.ToLower() == nameNormalized, cancellationToken);
        if (exists)
            throw new ConflictException("CUSTOMER_NAME_DUPLICATE", $"A customer with name '{request.Name}' already exists.");

        var customer = new Customer
        {
            StoreId = storeId,
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address?.Trim(),
            CreditLimit = request.CreditLimit,
            Notes = request.Notes?.Trim(),
            IsActive = true
        };

        _context.Customers.Add(customer);

        if (request.OpeningBalance.HasValue && request.OpeningBalance.Value > 0)
        {
            var tx = new CustomerAccountTransaction
            {
                StoreId = storeId,
                CustomerId = customer.Id,
                Type = CustomerTransactionType.OpeningBalance,
                Amount = request.OpeningBalance.Value,
                Notes = "Opening balance",
                CreatedBy = _userContext.CurrentUserId ?? Guid.Empty
            };
            _context.CustomerAccountTransactions.Add(tx);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(customer.Id, cancellationToken)
            ?? throw new DomainException("INTERNAL_ERROR", "Failed to retrieve created customer.");
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return null;

        var balance = await _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == id)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return new CustomerResponse(
            customer.Id,
            customer.Name,
            customer.Phone,
            customer.Address,
            customer.CreditLimit,
            customer.Notes,
            balance,
            customer.IsActive);
    }

    public async Task<CustomerListResponse> GetAllAsync(
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

        var query = _context.Customers.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.Phone.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var customerIds = customers.Select(c => c.Id).ToList();

        var balances = await _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => customerIds.Contains(t.CustomerId))
            .GroupBy(t => t.CustomerId)
            .Select(g => new { CustomerId = g.Key, Balance = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Balance, cancellationToken);

        var items = customers.Select(c =>
        {
            var bal = balances.TryGetValue(c.Id, out var b) ? b : 0m;
            return new CustomerResponse(
                c.Id,
                c.Name,
                c.Phone,
                c.Address,
                c.CreditLimit,
                c.Notes,
                bal,
                c.IsActive);
        }).ToList();

        return new CustomerListResponse(items, total, pageNumber, pageSize);
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
            return null;

        var nameNormalized = request.Name.Trim().ToLower();
        var duplicate = await _context.Customers.AnyAsync(c => c.Id != id && c.Name.ToLower() == nameNormalized, cancellationToken);
        if (duplicate)
            throw new ConflictException("CUSTOMER_NAME_DUPLICATE", $"A customer with name '{request.Name}' already exists.");

        customer.Name = request.Name.Trim();
        customer.Phone = request.Phone.Trim();
        customer.Address = request.Address?.Trim();
        customer.CreditLimit = request.CreditLimit;
        customer.Notes = request.Notes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
            return false;

        var hasSales = await _context.Sales.AnyAsync(s => s.CustomerId == id, cancellationToken);
        if (hasSales)
            throw new ConflictException("CUSTOMER_HAS_TRANSACTIONS", "Cannot delete customer with linked sales. Deactivate the customer instead.");

        var hasTransactions = await _context.CustomerAccountTransactions
            .AnyAsync(t => t.CustomerId == id && t.Type != CustomerTransactionType.OpeningBalance, cancellationToken);
        if (hasTransactions)
            throw new ConflictException("CUSTOMER_HAS_TRANSACTIONS", "Cannot delete customer with transaction history. Deactivate the customer instead.");

        customer.DeletedAt = DateTime.UtcNow;
        customer.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AccountStatementResponse?> GetStatementAsync(Guid customerId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
            return null;

        var query = _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId);

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

        return new AccountStatementResponse(customer.Id, customer.Name, running, items);
    }
}
