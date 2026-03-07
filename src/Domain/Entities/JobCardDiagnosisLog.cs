namespace Domain.Entities;

public sealed class JobCardDiagnosisLog : BaseEntity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    public string DiagnosisNote { get; set; } = string.Empty;
    public DateTimeOffset? EstimatedEta { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}
