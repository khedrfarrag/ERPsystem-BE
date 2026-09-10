namespace RetailOS.Application.Expenses.DTOs;

public record CreateExpenseCategoryRequest(
    string Name,
    string? Description = null);

public record ExpenseCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

public record CreateExpenseRequest(
    Guid CategoryId,
    decimal Amount,
    DateTimeOffset ExpenseDate,
    string PaymentMethod,
    string? Description = null);

public record ExpenseResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    DateTimeOffset ExpenseDate,
    string PaymentMethod,
    string? Description,
    DateTimeOffset CreatedAt);

public record ExpenseListResponse(
    IReadOnlyList<ExpenseResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
