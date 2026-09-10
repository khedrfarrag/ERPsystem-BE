using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Expenses.DTOs;
using RetailOS.Application.Expenses.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Operations;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreateExpenseCategoryRequest> _categoryValidator;
    private readonly IValidator<CreateExpenseRequest> _expenseValidator;

    public ExpenseService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IValidator<CreateExpenseCategoryRequest> categoryValidator,
        IValidator<CreateExpenseRequest> expenseValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _categoryValidator = categoryValidator;
        _expenseValidator = expenseValidator;
    }

    public async Task<ExpenseCategoryResponse> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _categoryValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var nameNormalized = request.Name.Trim().ToLower();

        var exists = await _context.ExpenseCategories.AnyAsync(c => c.Name.ToLower() == nameNormalized, cancellationToken);
        if (exists)
            throw new ConflictException("EXPENSE_CATEGORY_EXISTS", $"An expense category with name '{request.Name}' already exists.");

        var category = new ExpenseCategory
        {
            StoreId = storeId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true
        };

        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new ExpenseCategoryResponse(category.Id, category.Name, category.Description, category.IsActive);
    }

    public async Task<IReadOnlyList<ExpenseCategoryResponse>> GetCategoriesAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var query = _context.ExpenseCategories.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryResponse(c.Id, c.Name, c.Description, c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var val = await _expenseValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new DomainException("VALIDATION_ERROR", val.Errors[0].ErrorMessage, 400, val.Errors.Select(e => e.ErrorMessage));

        var storeId = _storeContext.CurrentStoreId!.Value;
        var category = await _context.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            throw new DomainException("CATEGORY_INACTIVE", "Expense category not found or inactive.");

        var pMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, true);
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        var expense = new Expense
        {
            StoreId = storeId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            PaymentMethod = pMethod,
            Description = request.Description?.Trim(),
            CreatedBy = userId
        };

        _context.Expenses.Add(expense);

        if (pMethod == PaymentMethod.Cash)
        {
            _context.CashRegisterTransactions.Add(new CashRegisterTransaction
            {
                StoreId = storeId,
                Type = CashTransactionType.CashExpense,
                Amount = -request.Amount,
                ReferenceId = expense.Id,
                Notes = $"Expense: {category.Name}",
                CreatedBy = userId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ExpenseResponse(
            expense.Id,
            expense.CategoryId,
            category.Name,
            expense.Amount,
            expense.ExpenseDate,
            expense.PaymentMethod.ToString(),
            expense.Description,
            expense.CreatedAt);
    }

    public async Task<ExpenseListResponse> GetAllExpensesAsync(
        Guid? categoryId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Expenses.AsNoTracking().Include(e => e.Category).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate <= to.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExpenseResponse(
                e.Id,
                e.CategoryId,
                e.Category.Name,
                e.Amount,
                e.ExpenseDate,
                e.PaymentMethod.ToString(),
                e.Description,
                e.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ExpenseListResponse(items, total, pageNumber, pageSize);
    }
}
