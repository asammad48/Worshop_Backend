using Application.DTOs.Printing;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Errors;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public sealed class ReceiptService : IReceiptService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public ReceiptService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<PublicJobCardReceiptResponse> GetPublicReceiptAsync(Guid jobCardId, string? token, CancellationToken ct = default)
    {
        bool requireToken = _config.GetValue<bool>("PublicReceipt:RequireToken", false);
        if (requireToken)
        {
            if (string.IsNullOrEmpty(token) || !ValidateToken(jobCardId, token))
            {
                throw new UnauthorizedException("Invalid receipt token");
            }
        }

        var job = await _db.JobCards
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == jobCardId && !x.IsDeleted, ct);

        if (job is null) throw new NotFoundException("Job card not found");

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(x => x.JobCardId == jobCardId && !x.IsDeleted, ct);

        var invoiceLines = await _db.JobLineItems
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .Select(x => new PublicReceiptInvoiceLineDto(x.Title, x.Qty, x.UnitPrice, x.Total))
            .ToListAsync(ct);

        decimal paid = 0;
        List<PublicReceiptPaymentDto> payments = new();
        if (invoice != null)
        {
            var dbPayments = await _db.Payments
                .Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted)
                .OrderBy(x => x.PaidAt)
                .ToListAsync(ct);

            paid = dbPayments.Sum(x => x.Amount);
            payments = dbPayments.Select(x => new PublicReceiptPaymentDto(x.PaidAt, x.Amount, x.Method.ToString())).ToList();
        }

        var publicInvoice = new PublicReceiptInvoiceDto(
            invoice != null,
            invoice?.Subtotal ?? 0,
            invoice?.Discount ?? 0,
            invoice?.Tax ?? 0,
            invoice?.Total ?? 0,
            paid,
            (invoice?.Total ?? 0) - paid,
            invoiceLines
        );

        var communications = await _db.CommunicationLogs
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderByDescending(x => x.OccurredAt)
            .Take(10)
            .Select(x => new PublicReceiptCommDto(x.OccurredAt, x.Summary))
            .ToListAsync(ct);

        return new PublicJobCardReceiptResponse(
            job.Id,
            job.Vehicle?.Plate ?? "N/A",
            job.Customer?.FullName ?? "N/A",
            job.Branch?.Name ?? "N/A",
            job.EntryAt ?? job.CreatedAt,
            job.ExitAt,
            job.RequestedEta,
            job.LatestEstimatedEta,
            job.Status.ToString(),
            publicInvoice,
            payments,
            communications
        );
    }

    private bool ValidateToken(Guid jobCardId, string token)
    {
        var expected = GenerateToken(jobCardId);
        return expected == token;
    }

    public string GenerateToken(Guid jobCardId)
    {
        var key = _config["Jwt:Key"] ?? "default-secret-key-for-receipts";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(jobCardId.ToString()));
        return Convert.ToHexString(hash).ToLower();
    }
}
