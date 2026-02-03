using Application.DTOs.Vehicles; using Application.Pagination;
namespace Application.Services.Interfaces;
public interface IVehicleService{Task<VehicleResponse>CreateAsync(VehicleCreateRequest r,CancellationToken ct=default);Task<PageResponse<VehicleResponse>>GetPagedAsync(PageRequest r,CancellationToken ct=default);Task<VehicleResponse>GetByIdAsync(Guid id,CancellationToken ct=default);} 
