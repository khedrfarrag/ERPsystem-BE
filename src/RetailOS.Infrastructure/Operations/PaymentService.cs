using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Payments.DTOs;
using RetailOS.Application.Payments.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreatePaymentRequest> _createValidator;

    public PaymentService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<CreatePaymentRequest> createValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _createValidator = createValidator;
    }

    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var pType = Enum.Parse<PaymentPartyType>(request.PartyType, true);
        var pMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, true);
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        var payment = new Payment
        {
            StoreId = storeId,
            PartyType = pType,
            CustomerId = request.CustomerId,
            SupplierId = request.SupplierId,
            Amount = request.Amount,
            PaymentDate = DateTimeOffset.UtcNow,
            PaymentMethod = pMethod,
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedBy = userId
        };

        if (pType == PaymentPartyType.Customer)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId!.Value, cancellationToken);
            if (customer is null || !customer.IsActive)
                throw new DomainException("CUSTOMER_INACTIVE", "Customer does not exist or is inactive.");

            _context.CustomerAccountTransactions.Add(new CustomerAccountTransaction
            {
                StoreId = storeId,
                CustomerId = customer.Id,
                Type = CustomerTransactionType.Payment,
                Amount = -request.Amount, // decreases balance
                ReferenceId = payment.Id,
                Notes = request.Notes ?? "Customer payment",
                CreatedBy = userId
            });

            if (pMethod == PaymentMethod.Cash)
            {
                _context.CashRegisterTransactions.Add(new CashRegisterTransaction
                {
                    StoreId = storeId,
                    Type = CashTransactionType.CashPaymentIn,
                    Amount = request.Amount,
                    ReferenceId = payment.Id,
                    Notes = $"Customer payment from {customer.Name}",
                    CreatedBy = userId
                });
            }
        }
        else // Supplier
        {
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId!.Value, cancellationToken);
            if (supplier is null || !supplier.IsActive)
                throw new DomainException("SUPPLIER_INACTIVE", "Supplier does not exist or is inactive.");

            _context.SupplierAccountTransactions.Add(new SupplierAccountTransaction
            {
                StoreId = storeId,
                SupplierId = supplier.Id,
                Type = SupplierTransactionType.Payment,
                Amount = -request.Amount, // decreases payable
                ReferenceId = payment.Id,
                Notes = request.Notes ?? "Supplier payment",
                CreatedBy = userId
            });

            if (pMethod == PaymentMethod.Cash)
            {
                _context.CashRegisterTransactions.Add(new CashRegisterTransaction
                {
                    StoreId = storeId,
                    Type = CashTransactionType.CashPaymentOut,
                    Amount = -request.Amount,
                    ReferenceId = payment.Id,
                    Notes = $"Supplier payment to {supplier.Name}",
                    CreatedBy = userId
                });
            }
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(payment.Id, cancellationToken)
            ?? throw new DomainException("INTERNAL_ERROR", "Failed to retrieve recorded payment.");
    }

    public async Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var p = await _context.Payments
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (p is null)
            return null;

        return new PaymentResponse(
            p.Id,
            p.PartyType.ToString(),
            p.CustomerId,
            p.Customer?.Name,
            p.SupplierId,
            p.Supplier?.Name,
            p.Amount,
            p.PaymentDate,
            p.PaymentMethod.ToString(),
            p.ReferenceNumber,
            p.Notes,
            p.CreatedAt);
    }

    public async Task<PaymentListResponse> GetAllAsync(
        string? partyType = null,
        Guid? partyId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Payments.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(partyType) && Enum.TryParse<PaymentPartyType>(partyType, true, out var pt))
            query = query.Where(p => p.PartyType == pt);

        if (partyId.HasValue)
            query = query.Where(p => p.CustomerId == partyId.Value || p.SupplierId == partyId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentResponse(
                p.Id,
                p.PartyType.ToString(),
                p.CustomerId,
                p.Customer != null ? p.Customer.Name : null,
                p.SupplierId,
                p.Supplier != null ? p.Supplier.Name : null,
                p.Amount,
                p.PaymentDate,
                p.PaymentMethod.ToString(),
                p.ReferenceNumber,
                p.Notes,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaymentListResponse(items, total, pageNumber, pageSize);
    }
}
