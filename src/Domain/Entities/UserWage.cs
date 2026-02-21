namespace Domain.Entities;

public sealed class UserWage : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public decimal HourlyRate { get; set; }
    public string Currency { get; set; } = "PKR";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
