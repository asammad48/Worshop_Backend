using Application.DTOs.Billing;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class BillingService : IBillingService
{
    private readonly AppDbContext _db;
    public BillingService(AppDbContext db) { _db = db; }

    public async Task<InvoiceResponse> CreateOrGetInvoiceAsync(Guid actorUserId, Guid branchId, Guid jobCardId, InvoiceCreateRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobCards.FirstOrDefaultAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        var inv = await _db.Invoices.FirstOrDefaultAsync(x => x.JobCardId == jobCardId && !x.IsDeleted, ct);
        if (inv is null)
        {
            if (request.Subtotal < 0 || request.Discount < 0 || request.Tax < 0)
                throw new ValidationException("Validation failed", new[] { "Amounts cannot be negative." });
            var subtotal = await _db.JobLineItems.Where(x => x.JobCardId == jobCardId && !x.IsDeleted).SumAsync(x => x.Total, ct);
            if (subtotal == 0 && request.Subtotal != 0)
                throw new ValidationException("Validation failed", new[] { "Subtotal must be 0 if there are no line items." });
            var discountAmount = subtotal * request.Discount / 100m;
            var discountedSubtotal = subtotal - discountAmount;

            var taxAmount = discountedSubtotal * request.Tax / 100m;

            var total = discountedSubtotal + taxAmount;
            inv = new Domain.Entities.Invoice
            {
                JobCardId = jobCardId,
                Subtotal = subtotal,
                Discount = request.Discount,
                Tax = request.Tax,
                Total = Math.Max(0, total),
                PaymentStatus = PaymentStatus.Pending
            };
            _db.Invoices.Add(inv);
            await _db.SaveChangesAsync(ct);
        }

        return Map(inv);
    }

    public async Task<InvoiceResponse> GetInvoiceAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        var inv = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.JobCardId == jobCardId && !x.IsDeleted, ct);
        if (inv is null) throw new NotFoundException("Invoice not found");
        return Map(inv);
    }

    public async Task<PaymentResponse> AddPaymentAsync(Guid actorUserId, Guid branchId, Guid invoiceId, PaymentCreateRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0) throw new ValidationException("Validation failed", new[] { "Amount must be > 0." });

        var inv = await _db.Invoices.Include(x => x.JobCard).FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, ct);
        if (inv is null) throw new NotFoundException("Invoice not found");
        if (inv.JobCard is null || inv.JobCard.BranchId != branchId) throw new ForbiddenException("Wrong branch");

        var pay = new Domain.Entities.Payment
        {
            InvoiceId = inv.Id,
            Amount = request.Amount,
            Method = request.Method,
            PaidAt = DateTimeOffset.UtcNow,
            ReceivedByUserId = actorUserId,
            Notes = request.Notes?.Trim()
        };
        _db.Payments.Add(pay);

        await _db.SaveChangesAsync(ct);

        await RecomputeInvoiceAsync(inv.JobCardId, actorUserId, ct);

        return new PaymentResponse(pay.Id, pay.InvoiceId, pay.Amount, pay.Method, pay.PaidAt, pay.ReceivedByUserId, pay.Notes);
    }

    public async Task<IReadOnlyList<PaymentResponse>> ListPaymentsAsync(Guid branchId, Guid invoiceId, CancellationToken ct = default)
    {
        var inv = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, ct);
        if (inv is null) throw new NotFoundException("Invoice not found");

        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == inv.JobCardId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");
        if (job.BranchId != branchId) throw new ForbiddenException("Wrong branch");

        return await _db.Payments.AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId && !x.IsDeleted)
            .OrderByDescending(x => x.PaidAt)
            .Select(x => new PaymentResponse(x.Id, x.InvoiceId, x.Amount, x.Method, x.PaidAt, x.ReceivedByUserId, x.Notes))
            .ToListAsync(ct);
    }

    public async Task<JobLineItemResponse> AddLineItemAsync(Guid actorUserId, Guid branchId, Guid jobCardId, JobLineItemCreateRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobCards.FirstOrDefaultAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        var line = new Domain.Entities.JobLineItem
        {
            JobCardId = jobCardId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Qty = request.Qty,
            UnitPrice = request.UnitPrice,
            Total = request.Qty * request.UnitPrice,
            Notes = request.Notes?.Trim(),
            PartId = request.PartId,
            JobPartRequestId = request.JobPartRequestId
        };
        _db.JobLineItems.Add(line);
        await _db.SaveChangesAsync(ct);

        await RecomputeInvoiceAsync(jobCardId, actorUserId, ct);

        return MapLine(line);
    }

    public async Task<IReadOnlyList<JobLineItemResponse>> ListLineItemsAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await _db.JobLineItems.AsNoTracking()
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => MapLine(x))
            .ToListAsync(ct);
    }

    public async Task DeleteLineItemAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var line = await _db.JobLineItems.Include(x => x.JobCard).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (line is null) throw new NotFoundException("Line item not found");
        if (line.JobCard?.BranchId != branchId) throw new ForbiddenException("Wrong branch");

        line.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        await RecomputeInvoiceAsync(line.JobCardId, actorUserId, ct);
    }

    private async Task RecomputeInvoiceAsync(Guid jobCardId, Guid actorUserId, CancellationToken ct)
    {
        var inv = await _db.Invoices.Include(x => x.JobCard).FirstOrDefaultAsync(x => x.JobCardId == jobCardId && !x.IsDeleted, ct);
        if (inv is null) return;

        var subtotal = await _db.JobLineItems.Where(x => x.JobCardId == jobCardId && !x.IsDeleted).SumAsync(x => x.Total, ct);
        inv.Subtotal = subtotal;

        var discountPct = Math.Clamp(inv.Discount, 0m, 100m);
        var taxPct = Math.Max(0m, inv.Tax); // if you allow >100% tax, keep it; otherwise clamp to 100 too

        var discountAmount = inv.Subtotal * discountPct / 100m;
        var discountedSubtotal = inv.Subtotal - discountAmount;

        // tax AFTER discount (standard). If you want tax on original subtotal, change base to inv.Subtotal.
        var taxAmount = discountedSubtotal * taxPct / 100m;

        inv.Total = Math.Max(0m, discountedSubtotal + taxAmount);

        // re-evaluate payment status
        var totalPaid = await _db.Payments.AsNoTracking().Where(x => x.InvoiceId == inv.Id && !x.IsDeleted).SumAsync(x => x.Amount, ct);
        if (totalPaid <= 0m) inv.PaymentStatus = PaymentStatus.Pending;
        else if (totalPaid < inv.Total) inv.PaymentStatus = PaymentStatus.PartiallyPaid;
        else inv.PaymentStatus = PaymentStatus.Paid;

        if (inv.PaymentStatus == PaymentStatus.Paid && inv.JobCard != null && inv.JobCard.Status != JobCardStatus.Pagado)
        {
            var oldStatus = inv.JobCard.Status;
            inv.JobCard.Status = JobCardStatus.Pagado;

            _db.AuditLogs.Add(new Domain.Entities.AuditLog
            {
                BranchId = inv.JobCard.BranchId,
                Action = "JOB_CARD_STATUS_CHANGE",
                EntityType = "JobCard",
                EntityId = inv.JobCard.Id,
                OldValue = oldStatus.ToString(),
                NewValue = $"{JobCardStatus.Pagado} (Auto from Billing)",
                PerformedByUserId = actorUserId,
                PerformedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private static InvoiceResponse Map(Domain.Entities.Invoice x) =>
        new(x.Id, x.JobCardId, x.Subtotal, x.Discount, x.Tax, x.Total, x.PaymentStatus);

    private static JobLineItemResponse MapLine(Domain.Entities.JobLineItem x) =>
        new(x.Id, x.JobCardId, x.Type, x.Title, x.Qty, x.UnitPrice, x.Total, x.Notes, x.PartId, x.JobPartRequestId);
}
