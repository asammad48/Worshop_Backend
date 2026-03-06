using Domain.Enums;

namespace Domain.Entities;

public sealed class CommunicationLog : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public CommunicationType Type { get; set; }
    public CommunicationDirection Direction { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string? Details { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}
