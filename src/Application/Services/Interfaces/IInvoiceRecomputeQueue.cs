namespace Application.Services.Interfaces;

public interface IInvoiceRecomputeQueue
{
    Task EnqueueAsync(Guid jobCardId, string? reason, CancellationToken ct = default);
}
