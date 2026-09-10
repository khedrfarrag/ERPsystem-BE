using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Units.DTOs;
using RetailOS.Application.Units.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Catalog;

public class UnitService : IUnitService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IValidator<CreateUnitRequest> _createValidator;
    private readonly IValidator<UpdateUnitRequest> _updateValidator;

    public UnitService(
        AppDbContext context,
        IStoreContext storeContext,
        IValidator<CreateUnitRequest> createValidator,
        IValidator<UpdateUnitRequest> updateValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<UnitListResponse> GetUnitsAsync(
        int page = 1,
        int pageSize = 25,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Units.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => MapToResponse(u))
            .ToListAsync(cancellationToken);

        return new UnitListResponse(items, totalCount, page, pageSize);
    }

    public async Task<UnitResponse> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var unit = await _context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit == null)
            throw new NotFoundException("UNIT_NOT_FOUND", "The requested unit was not found.");

        return MapToResponse(unit);
    }

    public async Task<UnitResponse> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var normalizedName = request.Name.Trim().ToLower();
        var normalizedSymbol = request.Symbol.Trim().ToLower();

        var nameExists = await _context.Units
            .AnyAsync(u => u.Name.ToLower() == normalizedName, cancellationToken);

        if (nameExists)
            throw new ConflictException("UNIT_NAME_CONFLICT", "A unit with this name already exists.");

        var symbolExists = await _context.Units
            .AnyAsync(u => u.Symbol.ToLower() == normalizedSymbol, cancellationToken);

        if (symbolExists)
            throw new ConflictException("UNIT_SYMBOL_CONFLICT", "A unit with this symbol already exists.");

        var unit = new Unit
        {
            StoreId = _storeContext.CurrentStoreId!.Value,
            Name = request.Name.Trim(),
            Symbol = request.Symbol.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(unit);
    }

    public async Task<UnitResponse> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var unit = await _context.Units
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit == null)
            throw new NotFoundException("UNIT_NOT_FOUND", "The requested unit was not found.");

        var normalizedName = request.Name.Trim().ToLower();
        var normalizedSymbol = request.Symbol.Trim().ToLower();

        var nameExists = await _context.Units
            .AnyAsync(u => u.Id != id && u.Name.ToLower() == normalizedName, cancellationToken);

        if (nameExists)
            throw new ConflictException("UNIT_NAME_CONFLICT", "A unit with this name already exists.");

        var symbolExists = await _context.Units
            .AnyAsync(u => u.Id != id && u.Symbol.ToLower() == normalizedSymbol, cancellationToken);

        if (symbolExists)
            throw new ConflictException("UNIT_SYMBOL_CONFLICT", "A unit with this symbol already exists.");

        unit.Name = request.Name.Trim();
        unit.Symbol = request.Symbol.Trim();
        unit.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(unit);
    }

    public async Task<UnitResponse> UpdateUnitStatusAsync(Guid id, UpdateUnitStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var unit = await _context.Units
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit == null)
            throw new NotFoundException("UNIT_NOT_FOUND", "The requested unit was not found.");

        unit.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(unit);
    }

    public async Task DeleteUnitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var unit = await _context.Units
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit == null)
            throw new NotFoundException("UNIT_NOT_FOUND", "The requested unit was not found.");

        var hasProducts = await _context.Products
            .AnyAsync(p => p.UnitId == id, cancellationToken);

        if (hasProducts)
            throw new ConflictException("UNIT_HAS_PRODUCTS", "Unit cannot be deleted while it has products assigned.");

        unit.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UnitResponse MapToResponse(Unit u) => new(
        u.Id,
        u.Name,
        u.Symbol,
        u.Description,
        u.IsActive,
        u.CreatedAt,
        u.UpdatedAt
    );
}
