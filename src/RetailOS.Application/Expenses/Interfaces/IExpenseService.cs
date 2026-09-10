using RetailOS.Application.Expenses.DTOs;

namespace RetailOS.Application.Expenses.Interfaces;

public interface IExpenseService
{
    Task<ExpenseCategoryResponse> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseCategoryResponse>> GetCategoriesAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseListResponse> GetAllExpensesAsync(Guid? categoryId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
