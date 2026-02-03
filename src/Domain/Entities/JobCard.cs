using Domain.Enums;
namespace Domain.Entities;
public sealed class JobCard : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public JobCardStatus Status { get; set; } = JobCardStatus.NuevaSolicitud;

    public DateTimeOffset? EntryAt { get; set; }
    public DateTimeOffset? ExitAt { get; set; }
    public int? Mileage { get; set; }
    public string? InitialReport { get; set; }
    public string? Diagnosis { get; set; }
}
