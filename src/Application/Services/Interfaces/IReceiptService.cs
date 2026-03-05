using Application.DTOs.Printing;

namespace Application.Services.Interfaces;

public interface IReceiptService
{
    Task<PublicJobCardReceiptResponse> GetPublicReceiptAsync(Guid jobCardId, string? token, CancellationToken ct = default);
}
