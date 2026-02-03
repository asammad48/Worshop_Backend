using Application.DTOs.Billing;

namespace Application.Services.Interfaces;

public interface IBillingService
{
    Task<InvoiceResponse> CreateOrGetInvoiceAsync(Guid actorUserId, Guid branchId, Guid jobCardId, InvoiceCreateRequest request, CancellationToken ct = default);
    Task<InvoiceResponse> GetInvoiceAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
    Task<PaymentResponse> AddPaymentAsync(Guid actorUserId, Guid branchId, Guid invoiceId, PaymentCreateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentResponse>> ListPaymentsAsync(Guid branchId, Guid invoiceId, CancellationToken ct = default);
}
