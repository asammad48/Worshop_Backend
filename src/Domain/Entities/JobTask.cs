using Domain.Enums;

namespace Domain.Entities;

public sealed class JobTask : BaseEntity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public string StationCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public Guid? StartedByUserId { get; set; }
    public Guid? EndedByUserId { get; set; }

    public int TotalMinutes { get; set; }
    public string? Notes { get; set; }
    public JobTaskStatus Status { get; set; } = JobTaskStatus.Pending;
}
