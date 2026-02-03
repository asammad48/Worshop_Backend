using Application.DTOs.WorkStations;
using Application.Pagination;

namespace Application.Services.Interfaces;

public interface IWorkStationService
{
    Task<WorkStationResponse> CreateAsync(Guid branchId, WorkStationCreateRequest request, CancellationToken ct = default);
    Task<PageResponse<WorkStationResponse>> GetPagedAsync(Guid branchId, PageRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<JobCardStationHistoryResponse>> GetJobCardHistoryAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
    Task<JobCardStationHistoryResponse> MoveJobCardAsync(Guid actorUserId, Guid branchId, Guid jobCardId, MoveJobCardRequest request, CancellationToken ct = default);
}
