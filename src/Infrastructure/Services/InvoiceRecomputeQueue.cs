using Application.Services.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class InvoiceRecomputeQueue : IInvoiceRecomputeQueue
{
    private readonly AppDbContext _db;

    public InvoiceRecomputeQueue(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueAsync(Guid jobCardId, string? reason, CancellationToken ct = default)
    {
        // Coalesce multiple jobs for same jobCardId if they are still Pending
        var existing = await _db.InvoiceRecomputeJobs
            .FirstOrDefaultAsync(x => x.JobCardId == jobCardId && x.Status == "Pending" && !x.IsDeleted, ct);

        if (existing != null)
        {
            if (!string.IsNullOrEmpty(reason) && !(existing.Reason ?? "").Contains(reason))
            {
                existing.Reason = (existing.Reason + "; " + reason).Trim(' ', ';');
            }
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            var job = new InvoiceRecomputeJob
            {
                JobCardId = jobCardId,
                Reason = reason,
                Status = "Pending",
                Attempts = 0
            };
            _db.InvoiceRecomputeJobs.Add(job);
        }

        await _db.SaveChangesAsync(ct);
    }
}
