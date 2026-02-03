using Application.DTOs.Approvals;

namespace Application.Services.Interfaces;

public interface IGenericApprovalService
{
    Task<ApprovalResponse> CreateAsync(Guid actorUserId, Guid branchId, ApprovalCreateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ApprovalResponse>> ListAsync(Guid branchId, string targetType, Guid targetId, CancellationToken ct = default);
}
