namespace Domain.Entities;

public sealed class InvoiceRecomputeJob : BaseEntity
{
    public Guid JobCardId { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Succeeded, Failed
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
