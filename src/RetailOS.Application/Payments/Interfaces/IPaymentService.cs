using RetailOS.Application.Payments.DTOs;

namespace RetailOS.Application.Payments.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentListResponse> GetAllAsync(string? partyType = null, Guid? partyId = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
