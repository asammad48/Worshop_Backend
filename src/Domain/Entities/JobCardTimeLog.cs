namespace Domain.Entities;

public sealed class JobCardTimeLog : BaseEntity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public Guid JobTaskId { get; set; }
    public JobTask? JobTask { get; set; }

    public Guid TechnicianUserId { get; set; }

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public int TotalMinutes { get; set; } // set on stop
}
