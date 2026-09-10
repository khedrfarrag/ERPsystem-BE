using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Expenses.DTOs;
using RetailOS.Application.Expenses.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExpenseCategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetCategoriesAsync(isActive, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ExpenseCategoryResponse>>.Ok(result));
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateExpenseCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.CreateCategoryAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ExpenseCategoryResponse>.Ok(result, "Expense category created."));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ExpenseListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.GetAllExpensesAsync(categoryId, from, to, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<ExpenseListResponse>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.CreateExpenseAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ExpenseResponse>.Ok(result, "Expense recorded successfully."));
    }
}
