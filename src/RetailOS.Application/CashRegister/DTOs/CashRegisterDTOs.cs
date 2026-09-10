namespace RetailOS.Application.CashRegister.DTOs;

public record OpenFloatRequest(
    decimal Amount,
    string? Notes = null);

public record CloseRegisterRequest(
    decimal CountedAmount,
    string? Notes = null);

public record CashRegisterSummaryResponse(
    decimal CurrentBalance,
    DateTimeOffset? LastFloatDate,
    decimal? LastFloatAmount,
    decimal TodayInflows,
    decimal TodayOutflows);

public record CashRegisterCloseResponse(
    decimal ExpectedBalance,
    decimal CountedAmount,
    decimal Discrepancy,
    string? Notes,
    DateTimeOffset ClosedAt);

public record CashRegisterTransactionResponse(
    Guid Id,
    DateTimeOffset Date,
    string Type,
    decimal Amount,
    Guid? ReferenceId,
    string? Notes);
