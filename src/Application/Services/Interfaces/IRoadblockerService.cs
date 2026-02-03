using Application.DTOs.Roadblockers;

namespace Application.Services.Interfaces;

public interface IRoadblockerService
{
    Task<RoadblockerResponse> CreateAsync(Guid actorUserId, Guid branchId, RoadblockerCreateRequest req, CancellationToken ct=default);
    Task<RoadblockerResponse> ResolveAsync(Guid actorUserId, Guid branchId, Guid roadblockerId, CancellationToken ct=default);
    Task<IReadOnlyList<RoadblockerResponse>> ListByJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct=default);
}
