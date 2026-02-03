using Application.DTOs.Communications;

namespace Application.Services.Interfaces;

public interface ICommunicationService
{
    Task<CommunicationLogResponse> CreateAsync(Guid actorUserId, Guid branchId, CommunicationLogCreateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CommunicationLogResponse>> ListByJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
}
