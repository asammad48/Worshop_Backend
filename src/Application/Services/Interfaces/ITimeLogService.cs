using Application.DTOs.TimeLogs;

namespace Application.Services.Interfaces;

public interface ITimeLogService
{
    Task<TimeLogResponse> StartAsync(Guid actorUserId, Guid branchId, Guid jobCardId, Guid technicianUserId,Guid jobTaskId ,CancellationToken ct = default);
    Task<TimeLogResponse> StopAsync(Guid actorUserId, Guid branchId, Guid jobCardId, Guid timeLogId, CancellationToken ct = default);
    Task<IReadOnlyList<TimeLogResponse>> ListAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
}
