namespace Domain.Entities;
public sealed class JobCardWorkStationHistory : BaseEntity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public Guid WorkStationId { get; set; }
    public WorkStation? WorkStation { get; set; }

    public DateTimeOffset MovedAt { get; set; }
    public Guid MovedByUserId { get; set; }
    public string? Notes { get; set; }
}
