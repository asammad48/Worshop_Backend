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
            .CountAsync(x => !x.IsDeleted && x.BranchId == branchId && x.SentAt >= from && x.SentAt <= to, ct);

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
}
