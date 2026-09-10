using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Stores.DTOs;
using RetailOS.Application.Stores.Interfaces;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Stores;

public class StoreService : IStoreService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IValidator<UpdateStoreRequest> _validator;

    public StoreService(
        AppDbContext context,
        IStoreContext storeContext,
        IValidator<UpdateStoreRequest> validator)
    {
        _context = context;
        _storeContext = storeContext;
        _validator = validator;
    }

    public async Task<StoreResponse> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context found in active session.");

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == _storeContext.CurrentStoreId, cancellationToken);

        if (store == null)
            throw new NotFoundException("STORE_NOT_FOUND", "The requested store does not exist.");

        return MapToResponse(store);
    }

    public async Task<StoreResponse> UpdateCurrentStoreAsync(UpdateStoreRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context found in active session.");

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == _storeContext.CurrentStoreId, cancellationToken);

        if (store == null)
            throw new NotFoundException("STORE_NOT_FOUND", "The requested store does not exist.");

        store.Name = request.Name.Trim();
        store.Phone = request.Phone?.Trim();
        store.Address = request.Address?.Trim();
        store.TaxEnabled = request.TaxEnabled;
        store.AllowNegativeStock = request.AllowNegativeStock;
        store.InvoicePrefix = request.InvoicePrefix?.Trim() ?? "INV";
        store.Currency = request.Currency.Trim().ToUpperInvariant();
        store.Timezone = request.Timezone.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(store);
    }

    private static StoreResponse MapToResponse(RetailOS.Domain.Entities.Store store) => new(
        store.Id,
        store.Name,
        store.BusinessType,
        store.Phone,
        store.Address,
        store.Currency,
        store.Timezone,
        store.TaxEnabled,
        store.AllowNegativeStock,
        store.InvoicePrefix,
        store.IsActive,
        store.CreatedAt
    );
}
