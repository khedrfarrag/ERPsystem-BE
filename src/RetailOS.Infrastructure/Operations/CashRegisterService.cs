using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.CashRegister.DTOs;
using RetailOS.Application.CashRegister.Interfaces;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class CashRegisterService : ICashRegisterService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<OpenFloatRequest> _openValidator;
    private readonly IValidator<CloseRegisterRequest> _closeValidator;

    public CashRegisterService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<OpenFloatRequest> openValidator,
        IValidator<CloseRegisterRequest> closeValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _openValidator = openValidator;
        _closeValidator = closeValidator;
    }

    public async Task<CashRegisterSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var storeId = _storeContext.CurrentStoreId!.Value;

        var currentBalance = await _context.CashRegisterTransactions
            .AsNoTracking()
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var today = DateTime.UtcNow.Date;

        var todayInflows = await _context.CashRegisterTransactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= today && t.Amount > 0m)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var todayOutflows = await _context.CashRegisterTransactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= today && t.Amount < 0m)
            .SumAsync(t => (decimal?)Math.Abs(t.Amount), cancellationToken) ?? 0m;

        var lastFloat = await _context.CashRegisterTransactions
            .AsNoTracking()
            .Where(t => t.Type == CashTransactionType.OpeningFloat)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new CashRegisterSummaryResponse(
            currentBalance,
            lastFloat?.CreatedAt,
            lastFloat?.Amount,
            todayInflows,
            todayOutflows);
    }

    public async Task<CashRegisterTransactionResponse> OpenFloatAsync(OpenFloatRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _openValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        var tx = new CashRegisterTransaction
        {
            StoreId = storeId,
            Type = CashTransactionType.OpeningFloat,
            Amount = request.Amount,
            Notes = request.Notes?.Trim() ?? "Morning opening float",
            CreatedBy = userId
        };

        _context.CashRegisterTransactions.Add(tx);
        await _context.SaveChangesAsync(cancellationToken);

        return new CashRegisterTransactionResponse(
            tx.Id,
            tx.CreatedAt,
            tx.Type.ToString(),
            tx.Amount,
            tx.ReferenceId,
            tx.Notes);
    }

    public async Task<CashRegisterCloseResponse> CloseRegisterAsync(CloseRegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _closeValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        var expectedBalance = await _context.CashRegisterTransactions
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var discrepancy = request.CountedAmount - expectedBalance;

        if (discrepancy != 0m)
        {
            var adj = new CashRegisterTransaction
            {
                StoreId = storeId,
                Type = CashTransactionType.CashAdjustment,
                Amount = discrepancy,
                Notes = $"EOD Count Discrepancy: {discrepancy} (Expected: {expectedBalance}, Counted: {request.CountedAmount})",
                CreatedBy = userId
            };
            _context.CashRegisterTransactions.Add(adj);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new CashRegisterCloseResponse(
            expectedBalance,
            request.CountedAmount,
            discrepancy,
            request.Notes,
            DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<CashRegisterTransactionResponse>> GetTransactionsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var query = _context.CashRegisterTransactions.AsNoTracking();

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CashRegisterTransactionResponse(
                t.Id,
                t.CreatedAt,
                t.Type.ToString(),
                t.Amount,
                t.ReferenceId,
                t.Notes))
            .ToListAsync(cancellationToken);
    }
}
