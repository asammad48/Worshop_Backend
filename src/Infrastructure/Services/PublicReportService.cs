using Application.DTOs.Printing;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Errors;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public sealed class PublicReportService : IPublicReportService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public PublicReportService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<JobCardPrintResponse> GetPublicFullJobCardReportAsync(Guid jobCardId, string? token, CancellationToken ct = default)
    {
        bool requireToken = _config.GetValue<bool>("PublicReceipt:RequireToken", false);
        if (requireToken && (string.IsNullOrEmpty(token) || !ValidateToken(jobCardId, token)))
            throw new UnauthorizedException("Invalid receipt token");

        var job = await _db.JobCards
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == jobCardId && !x.IsDeleted, ct);

        if (job is null) throw new NotFoundException("Job card not found");

        var header = new JobCardPrintHeaderDto(
            job.Id,
            job.Id.ToString().Substring(0, 8).ToUpper(),
            job.Vehicle?.Plate ?? "N/A",
            job.Customer?.FullName ?? "N/A",
            job.Customer?.Phone,
            job.Branch?.Name ?? "N/A",
            job.EntryAt ?? job.CreatedAt,
            job.ExitAt,
            (int)((job.ExitAt ?? DateTimeOffset.UtcNow) - (job.EntryAt ?? job.CreatedAt)).TotalDays,
            job.Status.ToString(),
            job.InitialReport
        );

        var tasks = await (from t in _db.JobTasks.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                           join u in _db.Users on t.StartedByUserId equals u.Id into users
                           from u in users.DefaultIfEmpty()
                           select new JobCardPrintTaskDto(t.Id, t.Title, t.Status.ToString(), ComputeDisplayStatus(job.EntryAt, job.ExitAt, t.Status), u.Email, t.StartedAt, t.EndedAt, t.Notes))
                           .ToListAsync(ct);

        var partsUsed = await (from u in _db.JobCardPartUsages.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                               join p in _db.Parts on u.PartId equals p.Id
                               join l in _db.Locations on u.LocationId equals l.Id
                               select new JobCardPrintPartDto(p.Sku, p.Name, u.QuantityUsed, u.UnitPrice ?? 0, u.QuantityUsed * (u.UnitPrice ?? 0), l.Name, u.UsedAt))
                               .ToListAsync(ct);

        var partRequests = await (from r in _db.JobPartRequests.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                  join p in _db.Parts on r.PartId equals p.Id
                                  join u in _db.Users on r.CreatedBy equals u.Id into users
                                  from u in users.DefaultIfEmpty()
                                  join s in _db.Suppliers on r.SupplierId equals s.Id into suppliers
                                  from s in suppliers.DefaultIfEmpty()
                                  select new JobCardPrintPartRequestDto(p.Sku, p.Name, r.Qty, r.Status.ToString(), u.Email, r.RequestedAt, s.Name, null))
                                  .ToListAsync(ct);

        var roadblockers = await (from rb in _db.Roadblockers.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                  join u in _db.Users on rb.CreatedByUserId equals u.Id
                                  join ru in _db.Users on rb.ResolvedByUserId equals ru.Id into rusers
                                  from ru in rusers.DefaultIfEmpty()
                                  select new JobCardPrintRoadblockerDto(rb.Type.ToString(), rb.IsResolved ? "Resolved" : "Active", u.Email, rb.CreatedAt, ru.Email, rb.ResolvedAt, rb.Description))
                                  .ToListAsync(ct);

        var timeLogs = await (from tl in _db.JobCardTimeLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                              join u in _db.Users on tl.TechnicianUserId equals u.Id
                              join t in _db.JobTasks on tl.JobTaskId equals t.Id into tasks_
                              from t in tasks_.DefaultIfEmpty()
                              select new JobCardPrintTimeLogDto(u.Email, t.Title, tl.StartAt, tl.EndAt, tl.TotalMinutes))
                              .ToListAsync(ct);

        var taskWorkerTimeRows = await (from tl in _db.JobCardTimeLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                        join u in _db.Users on tl.TechnicianUserId equals u.Id
                                        join t in _db.JobTasks on tl.JobTaskId equals t.Id into tasks_
                                        from t in tasks_.DefaultIfEmpty()
                                        group tl by new { TaskTitle = t != null ? t.Title : "Unknown Task", WorkerEmail = u.Email } into g
                                        select new
                                        {
                                            g.Key.TaskTitle,
                                            g.Key.WorkerEmail,
                                            TotalMinutes = g.Sum(x => x.TotalMinutes)
                                        })
                                        .OrderBy(x => x.TaskTitle)
                                        .ThenBy(x => x.WorkerEmail)
                                        .ToListAsync(ct);

        var taskWorkerTimes = taskWorkerTimeRows
            .Select(x => new JobCardTaskWorkerTimeDto(
                x.TaskTitle,
                x.WorkerEmail,
                x.TotalMinutes,
                Math.Round(x.TotalMinutes / 60m, 2)))
            .ToList();

        var currentGarage = await _db.JobCardWorkStationHistories
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderByDescending(x => x.MovedAt)
            .Select(x => x.WorkStation != null ? x.WorkStation.Name : null)
            .FirstOrDefaultAsync(ct);

        var communications = await (from c in _db.CommunicationLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                    join u in _db.Users on c.CreatedByUserId equals u.Id
                                    select new JobCardPrintCommunicationDto(c.Type.ToString(), c.Direction.ToString(), c.Summary, c.Details, c.OccurredAt, u.Email))
                                    .ToListAsync(ct);

        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.JobCardId == jobCardId && !x.IsDeleted, ct);
        decimal paid = invoice == null ? 0 : await _db.Payments.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).SumAsync(x => x.Amount, ct);

        var financial = new JobCardPrintFinancialDto(
            invoice != null,
            invoice?.Id.ToString().Substring(0, 8),
            invoice?.Subtotal ?? 0,
            invoice?.Discount ?? 0,
            invoice?.Tax ?? 0,
            invoice?.Total ?? 0,
            paid,
            (invoice?.Total ?? 0) - paid
        );

        return new JobCardPrintResponse(header, job.Diagnosis, job.LatestDiagnosisSummary, job.RequestedEta, job.LatestEstimatedEta, currentGarage, partRequests.Count, partsUsed.Count, tasks, taskWorkerTimes, partsUsed, partRequests, roadblockers, timeLogs, communications, financial);
    }

    private static string ComputeDisplayStatus(DateTimeOffset? entryAt, DateTimeOffset? exitAt, JobTaskStatus taskStatus)
    {
        if (!entryAt.HasValue) return "Pending";
        if (entryAt.HasValue && exitAt.HasValue) return "Completed";
        if (taskStatus == JobTaskStatus.Done) return "Completed";
        return "InProgress";
    }

    private bool ValidateToken(Guid jobCardId, string token) => GenerateToken(jobCardId) == token;

    private string GenerateToken(Guid jobCardId)
    {
        var key = _config["Jwt:Key"] ?? "default-secret-key-for-receipts";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(jobCardId.ToString()));
        return Convert.ToHexString(hash).ToLower();
    }
}
