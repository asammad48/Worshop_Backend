namespace Domain.Entities;
public sealed class Vehicle : BaseEntity
{
    public string Plate { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
