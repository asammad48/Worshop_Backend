using Application.DTOs.Approvals;
using Domain.Enums;

namespace Application.Services.Interfaces;

public interface IApprovalService
{
    Task<JobCardApprovalResponse> ApproveAsync(Guid actorUserId, Guid branchId, Guid jobCardId, ApprovalRole role, string? notes, CancellationToken ct = default);
    Task<IReadOnlyList<JobCardApprovalResponse>> ListAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
}
