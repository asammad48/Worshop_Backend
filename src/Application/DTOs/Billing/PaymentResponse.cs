using Domain.Enums;
namespace Application.DTOs.Billing;
public sealed record PaymentResponse(Guid Id,Guid InvoiceId,decimal Amount,PaymentMethod Method,DateTimeOffset PaidAt,Guid ReceivedByUserId,string? Notes);
