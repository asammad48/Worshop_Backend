using Application.DTOs.Printing;

namespace Application.Services.Interfaces;

public interface IPublicReportService
{
    Task<JobCardPrintResponse> GetPublicFullJobCardReportAsync(Guid jobCardId, string? token, CancellationToken ct = default);
}
