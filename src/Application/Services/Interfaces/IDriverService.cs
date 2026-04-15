using Application.DTOs.Drivers;
using Application.Pagination;

namespace Application.Services.Interfaces;

public interface IDriverService
{
    Task<DriverResponse> CreateAsync(DriverCreateRequest request, CancellationToken ct = default);
    Task<PageResponse<DriverResponse>> GetPagedAsync(DriverQueryRequest request, CancellationToken ct = default);
    Task<DriverResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
}
