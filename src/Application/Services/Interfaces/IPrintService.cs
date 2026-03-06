using Application.DTOs.Printing;

namespace Application.Services.Interfaces;

public interface IPrintService
{
    Task<byte[]> RenderJobCardPdfAsync(Guid jobCardId, Guid branchId, CancellationToken ct = default);
    Task<byte[]> RenderInvoicePdfAsync(Guid invoiceId, Guid branchId, CancellationToken ct = default);
    Task<byte[]> RenderPublicReceiptPdfAsync(Guid jobCardId, string? token, CancellationToken ct = default);
}
