using Domain.Enums;
namespace Domain.Entities;

public sealed class Invoice : BaseEntity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public int LaborMinutes { get; set; }
    public decimal LaborAmount { get; set; }
    public decimal LaborRatePerHour { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
}
