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
}
