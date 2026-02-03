using Application.DTOs.JobCards;

namespace Application.Services.Interfaces;

public interface IJobCardPartsService
{
    Task<JobCardPartUsageResponse> UsePartAsync(Guid actorUserId, Guid branchId, Guid jobCardId, JobCardPartUseRequest req, CancellationToken ct=default);
    Task<IReadOnlyList<JobCardPartUsageResponse>> ListAsync(Guid branchId, Guid jobCardId, CancellationToken ct=default);
}
