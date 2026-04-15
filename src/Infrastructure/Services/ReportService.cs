using Application.DTOs.Reports;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) { _db = db; }

    public async Task<SummaryReportResponse> GetSummaryAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var expenses = await _db.Expenses.AsNoTracking()
            .Where(x => !x.IsDeleted && x.BranchId == branchId && x.ExpenseAt >= from && x.ExpenseAt <= to)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var wages = await _db.WagePayments.AsNoTracking()
            .Where(x => !x.IsDeleted && x.BranchId == branchId && x.PaidAt >= from && x.PaidAt <= to)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var inShop = await _db.JobCards.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.BranchId == branchId && x.EntryAt != null && x.ExitAt == null, ct);

        var rb = await _db.Roadblockers.AsNoTracking()
            .Where(x => !x.IsDeleted && x.JobCardId != Guid.Empty)
            .Join(_db.JobCards.AsNoTracking().Where(j => j.BranchId == branchId && !j.IsDeleted),
                r => r.JobCardId, j => j.Id, (r, j) => r)
            .Where(x => x.CreatedAtLocal >= from && x.CreatedAtLocal <= to)
            .GroupBy(x => x.Type)
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        var commsCount = await _db.CommunicationLogs.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.BranchId == branchId && x.OccurredAt >= from && x.OccurredAt <= to, ct);

        var dict = rb.ToDictionary(x => x.Key, x => x.Count);
        return new SummaryReportResponse(expenses, wages, inShop, dict, commsCount);
    }

    public async Task<IReadOnlyList<StuckVehicleResponse>> GetStuckVehiclesAsync(Guid branchId, CancellationToken ct = default)
    {
        return await _db.JobCards.AsNoTracking()
            .Where(x => !x.IsDeleted && x.BranchId == branchId && x.EntryAt != null && x.ExitAt == null)
            .OrderBy(x => x.EntryAt)
            .Select(x => new StuckVehicleResponse(x.Id, x.VehicleId, x.EntryAt, x.Status.ToString()))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RoadblockerAgingResponse>> GetRoadblockersAgingAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.Roadblockers.AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsResolved && x.CreatedAtLocal >= from && x.CreatedAtLocal <= to)
            .Join(_db.JobCards.AsNoTracking().Where(j => j.BranchId == branchId && !j.IsDeleted),
                r => r.JobCardId, j => j.Id, (r, j) => r)
            .Select(x => new RoadblockerAgingResponse(
                x.Id,
                x.JobCardId,
                x.Type.ToString(),
                x.Description ?? "",
                x.CreatedAtLocal,
                (int)(now - x.CreatedAtLocal).TotalDays))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StationTimeResponse>> GetStationTimeAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var tasks = await _db.JobTasks.AsNoTracking()
            .Where(x => !x.IsDeleted && x.StartedAt != null && x.StartedAt >= from && x.StartedAt <= to)
            .Join(_db.JobCards.AsNoTracking().Where(j => j.BranchId == branchId && !j.IsDeleted),
                t => t.JobCardId, j => j.Id, (t, j) => t)
            .ToListAsync(ct);

        return tasks
            .GroupBy(x => new
            {
                x.StationCode,
                Year = x.StartedAt!.Value.Year,
                Week = System.Globalization.ISOWeek.GetWeekOfYear(x.StartedAt.Value.DateTime)
            })
            .Select(g => new StationTimeResponse(
                g.Key.StationCode,
                g.Key.Year,
                g.Key.Week,
                g.Sum(x => x.TotalMinutes)))
            .OrderBy(x => x.Year).ThenBy(x => x.WeekNumber).ThenBy(x => x.StationCode)
            .ToList();
    }

    public async Task<JobCardReportResponse> GetJobCardReportAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var items = await _db.JobCards.AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.BranchId == branchId
                && x.CreatedAt >= from
                && x.CreatedAt <= to)
            .Select(x => new JobCardReportItemResponse(
                x.Id,
                x.CreatedAt,
                x.Status.ToString(),
                x.EntryAt,
                x.ExitAt,
                x.Mileage,
                x.CustomerId,
                x.Customer != null ? x.Customer.FullName : null,
                x.Customer != null ? x.Customer.CustomerType.ToString() : Domain.Enums.CustomerType.Simple.ToString(),
                x.VehicleId,
                x.Vehicle != null ? x.Vehicle.Plate : null,
                x.Vehicle != null ? x.Vehicle.Make : null,
                x.Vehicle != null ? x.Vehicle.Model : null,
                x.Vehicle != null ? x.Vehicle.Year : null,
                x.InitialReport,
                x.Diagnosis,
                x.LatestDiagnosisSummary,
                x.RequestedEta,
                x.LatestEstimatedEta,
                x.LatestEstimatedPrice,
                _db.JobCardWorkStationHistories
                    .Where(h => !h.IsDeleted && h.JobCardId == x.Id)
                    .OrderByDescending(h => h.MovedAt)
                    .Select(h => h.WorkStation != null ? h.WorkStation.Code : null)
                    .FirstOrDefault(),
                _db.JobCardWorkStationHistories
                    .Where(h => !h.IsDeleted && h.JobCardId == x.Id)
                    .OrderByDescending(h => h.MovedAt)
                    .Select(h => h.WorkStation != null ? h.WorkStation.Name : null)
                    .FirstOrDefault(),
                _db.JobCardWorkStationHistories
                    .Where(h => !h.IsDeleted && h.JobCardId == x.Id)
                    .OrderByDescending(h => h.MovedAt)
                    .Select(h => (DateTimeOffset?)h.MovedAt)
                    .FirstOrDefault(),
                _db.JobCardWorkStationHistories.Count(h => !h.IsDeleted && h.JobCardId == x.Id),
                _db.JobCardDiagnosisLogs.Count(l => !l.IsDeleted && l.JobCardId == x.Id),
                _db.Invoices.Where(i => !i.IsDeleted && i.JobCardId == x.Id).Select(i => (decimal?)i.Total).FirstOrDefault(),
                _db.Invoices.Where(i => !i.IsDeleted && i.JobCardId == x.Id).Select(i => i.PaymentStatus.ToString()).FirstOrDefault()
            ))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return new JobCardReportResponse(items, items.Count, from, to);
    }
}
