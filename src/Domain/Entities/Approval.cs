using Domain.Enums;

namespace Domain.Entities;

public sealed class Approval : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }

    public ApprovalRole ApprovalType { get; set; }

    public Guid ApprovedByUserId { get; set; }
    public DateTimeOffset ApprovedAt { get; set; }

    public string? Note { get; set; }
    public ApprovalStatus Status { get; set; }
}
