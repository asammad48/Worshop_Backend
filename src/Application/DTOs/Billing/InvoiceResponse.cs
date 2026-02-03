using Domain.Enums;
namespace Application.DTOs.Billing;
public sealed record InvoiceResponse(Guid Id,Guid JobCardId,decimal Subtotal,decimal Discount,decimal Tax,decimal Total,PaymentStatus PaymentStatus);
