using Domain.Enums;
namespace Domain.Entities;

public sealed class JobCardApproval : BaseEntity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    public ApprovalRole Role { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public DateTimeOffset ApprovedAt { get; set; }
    public string? Notes { get; set; }
}
