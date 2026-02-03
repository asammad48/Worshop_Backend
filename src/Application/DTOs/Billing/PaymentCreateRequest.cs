using Domain.Enums;
namespace Application.DTOs.Billing;
public sealed record PaymentCreateRequest(decimal Amount, PaymentMethod Method, string? Notes);
