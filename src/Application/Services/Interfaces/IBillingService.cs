using Application.DTOs.Billing;

namespace Application.Services.Interfaces;

public interface IBillingService
{
    Task<InvoiceResponse> CreateOrGetInvoiceAsync(Guid actorUserId, Guid branchId, Guid jobCardId, InvoiceCreateRequest request, CancellationToken ct = default);
    Task<InvoiceResponse> GetInvoiceAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
    Task<PaymentResponse> AddPaymentAsync(Guid actorUserId, Guid branchId, Guid invoiceId, PaymentCreateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentResponse>> ListPaymentsAsync(Guid branchId, Guid invoiceId, CancellationToken ct = default);

    Task<JobLineItemResponse> AddLineItemAsync(Guid actorUserId, Guid branchId, Guid jobCardId, JobLineItemCreateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<JobLineItemResponse>> ListLineItemsAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default);
    Task DeleteLineItemAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default);
}
