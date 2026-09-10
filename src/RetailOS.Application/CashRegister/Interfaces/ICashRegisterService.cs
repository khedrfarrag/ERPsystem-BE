using RetailOS.Application.CashRegister.DTOs;

namespace RetailOS.Application.CashRegister.Interfaces;

public interface ICashRegisterService
{
    Task<CashRegisterSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<CashRegisterTransactionResponse> OpenFloatAsync(OpenFloatRequest request, CancellationToken cancellationToken = default);
    Task<CashRegisterCloseResponse> CloseRegisterAsync(CloseRegisterRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashRegisterTransactionResponse>> GetTransactionsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
}
