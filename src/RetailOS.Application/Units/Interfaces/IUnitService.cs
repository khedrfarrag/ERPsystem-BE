using RetailOS.Application.Units.DTOs;

namespace RetailOS.Application.Units.Interfaces;

public interface IUnitService
{
    Task<UnitListResponse> GetUnitsAsync(int page = 1, int pageSize = 25, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<UnitResponse> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitResponse> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitResponse> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitResponse> UpdateUnitStatusAsync(Guid id, UpdateUnitStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteUnitAsync(Guid id, CancellationToken cancellationToken = default);
}
