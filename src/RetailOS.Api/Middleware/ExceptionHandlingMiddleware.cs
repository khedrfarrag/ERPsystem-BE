using System.Net;
using System.Text.Json;
using FluentValidation;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Shared;

namespace RetailOS.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ApiResponse<object> errorResponse;

        switch (exception)
        {
            case DomainException domainEx:
                response.StatusCode = domainEx.StatusCode;
                errorResponse = ApiResponse<object>.Fail(domainEx.Code, domainEx.Message, domainEx.Errors);
                _logger.LogWarning(domainEx, "Domain exception handled: {Code} - {Message}", domainEx.Code, domainEx.Message);
                break;

            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
                errorResponse = ApiResponse<object>.Fail("VALIDATION_ERROR", "One or more validation errors occurred.", errors);
                _logger.LogWarning("Validation failed with {Count} errors.", errors.Count);
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = ApiResponse<object>.Fail("UNAUTHORIZED", "Unauthorized access.");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = ApiResponse<object>.Fail("INTERNAL_SERVER_ERROR", "An unexpected error occurred. Please try again later.");
                _logger.LogError(exception, "Unhandled exception occurred while processing request {Path}", context.Request.Path);
                break;
        }

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(json);
    }
}
