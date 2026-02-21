namespace Application.Services.Interfaces;

public interface IInvoiceComputationService
{
    Task RecomputeAsync(Guid jobCardId, string? reason, CancellationToken ct = default);
}
