namespace Domain.Entities;

public sealed class Driver : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
}
