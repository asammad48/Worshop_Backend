namespace Application.DTOs.Billing;
public sealed record InvoiceCreateRequest(decimal Subtotal, decimal Discount, decimal Tax);
