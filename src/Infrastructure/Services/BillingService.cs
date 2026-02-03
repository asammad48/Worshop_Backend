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

            inv = new Domain.Entities.Invoice
            {
                JobCardId = jobCardId,
                Subtotal = request.Subtotal,
                Discount = request.Discount,
                Tax = request.Tax,
                Total = Math.Max(0, request.Subtotal - request.Discount + request.Tax),
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

        // recompute payment status
        var totalPaid = await _db.Payments.AsNoTracking().Where(x => x.InvoiceId == inv.Id && !x.IsDeleted).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        if (totalPaid <= 0m) inv.PaymentStatus = PaymentStatus.Pending;
        else if (totalPaid < inv.Total) inv.PaymentStatus = PaymentStatus.PartiallyPaid;
        else inv.PaymentStatus = PaymentStatus.Paid;

        if (inv.PaymentStatus == PaymentStatus.Paid)
        {
            inv.JobCard!.Status = JobCardStatus.Pagado;
        }

        await _db.SaveChangesAsync(ct);

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

    private static InvoiceResponse Map(Domain.Entities.Invoice x) =>
        new(x.Id, x.JobCardId, x.Subtotal, x.Discount, x.Tax, x.Total, x.PaymentStatus);
}
