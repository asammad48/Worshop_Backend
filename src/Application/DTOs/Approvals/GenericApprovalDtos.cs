using Domain.Enums;

namespace Application.DTOs.Approvals;

public sealed record ApprovalCreateRequest(
    string TargetType,
    Guid TargetId,
    ApprovalRole ApprovalType,
    ApprovalStatus Status,
    string? Note);

public sealed record ApprovalResponse(
    Guid Id,
    Guid BranchId,
    string TargetType,
    Guid TargetId,
    ApprovalRole ApprovalType,
    Guid ApprovedByUserId,
    DateTimeOffset ApprovedAt,
    string? Note,
    ApprovalStatus Status);
