using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class InvoiceComputationService : IInvoiceComputationService
{
    private readonly AppDbContext _db;

    public InvoiceComputationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RecomputeAsync(Guid jobCardId, string? reason, CancellationToken ct = default)
    {
        var job = await _db.JobCards.FirstOrDefaultAsync(x => x.Id == jobCardId && !x.IsDeleted, ct);
        if (job == null) return;

        var inv = await _db.Invoices.FirstOrDefaultAsync(x => x.JobCardId == jobCardId && !x.IsDeleted, ct);
        if (inv == null) return;

        var timeLogs = await _db.JobCardTimeLogs
            .Where(x => x.JobCardId == jobCardId && x.EndAt != null && !x.IsDeleted)
            .ToListAsync(ct);

        decimal laborAmount = 0;
        int laborMinutes = 0;

        foreach (var log in timeLogs)
        {
            var date = log.StartAt.Date;
            var wageRate = await _db.UserWages
                .Where(x => x.UserId == log.TechnicianUserId && !x.IsDeleted)
                .Where(x => (x.EffectiveFrom == null || x.EffectiveFrom <= date) && (x.EffectiveTo == null || x.EffectiveTo >= date))
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => x.HourlyRate)
                .FirstOrDefaultAsync(ct);

            laborAmount += (log.TotalMinutes / 60m) * wageRate;
            laborMinutes += log.TotalMinutes;
        }

        inv.LaborMinutes = laborMinutes;
        inv.LaborAmount = Math.Round(laborAmount, 2);
        inv.LaborRatePerHour = laborMinutes > 0 ? Math.Round(laborAmount / (laborMinutes / 60m), 2) : 0;

        var lineItemsSum = await _db.JobLineItems
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .SumAsync(x => x.Total, ct);

        inv.Subtotal = lineItemsSum + inv.LaborAmount;
        inv.Total = Math.Max(0, inv.Subtotal - inv.Discount + inv.Tax);

        // re-evaluate payment status
        var totalPaid = await _db.Payments.Where(x => x.InvoiceId == inv.Id && !x.IsDeleted).SumAsync(x => x.Amount, ct);
        if (totalPaid <= 0m) inv.PaymentStatus = PaymentStatus.Pending;
        else if (totalPaid < inv.Total) inv.PaymentStatus = PaymentStatus.PartiallyPaid;
        else inv.PaymentStatus = PaymentStatus.Paid;

        // Auto-status update for JobCard if paid
        if (inv.PaymentStatus == PaymentStatus.Paid && job.Status != JobCardStatus.Pagado)
        {
            var oldStatus = job.Status;
            job.Status = JobCardStatus.Pagado;

            _db.AuditLogs.Add(new AuditLog
            {
                BranchId = job.BranchId,
                Action = "JOB_CARD_STATUS_CHANGE",
                EntityType = "JobCard",
                EntityId = job.Id,
                OldValue = oldStatus.ToString(),
                NewValue = $"{JobCardStatus.Pagado} (Auto from Invoice Recompute)",
                PerformedByUserId = Guid.Empty,
                PerformedAt = DateTimeOffset.UtcNow
            });
        }

        _db.AuditLogs.Add(new AuditLog
        {
            BranchId = job.BranchId,
            Action = "INVOICE_RECOMPUTE",
            EntityType = "Invoice",
            EntityId = inv.Id,
            OldValue = reason,
            NewValue = $"Labor: {inv.LaborAmount} ({inv.LaborMinutes}m), Subtotal: {inv.Subtotal}, Total: {inv.Total}",
            PerformedByUserId = Guid.Empty,
            PerformedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }
}
