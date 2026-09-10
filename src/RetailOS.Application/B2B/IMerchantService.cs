using RetailOS.Application.B2B.DTOs;
using RetailOS.Application.Suppliers.DTOs;

namespace RetailOS.Application.B2B;

public interface IMerchantService
{
    Task<MerchantListResponse> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<MerchantDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MerchantDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MerchantDto> CreateAsync(CreateMerchantRequest request, CancellationToken cancellationToken = default);
    Task<MerchantDto> UpdateAsync(Guid id, UpdateMerchantRequest request, CancellationToken cancellationToken = default);
    Task ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountStatementResponse?> GetStatementAsync(Guid id, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
    Task<AccountStatementResponse?> GetMyStatementAsync(Guid userId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
}

