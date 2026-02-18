using Domain.Enums;
namespace Application.DTOs.Approvals;
public sealed record JobCardApprovalResponse(
    Guid Id,
    Guid JobCardId,
    ApprovalRole Role,
    Guid ApprovedByUserId,
    DateTimeOffset ApprovedAt,
    string? Notes,
    string? RequestedByEmail = null,
    string? ApprovedByEmail = null);
