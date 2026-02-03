using Application.DTOs.JobTasks;

namespace Application.Services.Interfaces;

public interface IJobTaskService
{
    Task<JobTaskResponse> CreateAsync(Guid actorUserId, Guid branchId, JobTaskCreateRequest request, CancellationToken ct = default);
    Task<JobTaskResponse> StartAsync(Guid actorUserId, Guid branchId, Guid taskId, CancellationToken ct = default);
    Task<JobTaskResponse> StopAsync(Guid actorUserId, Guid branchId, Guid taskId, CancellationToken ct = default);
    Task<IReadOnlyList<JobTaskResponse>> ListByJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
}
