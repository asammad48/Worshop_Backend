using Domain.Enums;
namespace Domain.Entities;

public sealed class Payment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public Guid ReceivedByUserId { get; set; }
    public string? Notes { get; set; }
}
