using Application.DTOs.JobPartRequests;

namespace Application.Services.Interfaces;

public interface IPartRequestService
{
    Task<JobPartRequestResponse> CreateAsync(Guid actorUserId, Guid branchId, Guid jobCardId, JobPartRequestCreateRequest r, CancellationToken ct = default);
    Task<JobPartRequestResponse> MarkOrderedAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default);
    Task<JobPartRequestResponse> MarkArrivedAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default);
    Task<JobPartRequestResponse> StationSignAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default);
    Task<JobPartRequestResponse> OfficeSignAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JobPartRequestResponse>> ListForJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
}
