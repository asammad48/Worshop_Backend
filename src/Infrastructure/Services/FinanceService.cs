using Application.DTOs.Finance;
using Application.Pagination;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly AppDbContext _db;
    public FinanceService(AppDbContext db) { _db = db; }

    public async Task<ExpenseResponse> CreateExpenseAsync(Guid actorUserId, Guid branchId, ExpenseCreateRequest r, CancellationToken ct = default)
    {
        if (r.Amount <= 0) throw new ValidationException("Validation failed", new[] { "Amount must be > 0." });
        var e = new Domain.Entities.Expense
        {
            BranchId = branchId,
            Category = r.Category,
            Amount = r.Amount,
            Description = r.Description?.Trim(),
            ExpenseAt = r.ExpenseAt,
            CreatedByUserId = actorUserId
        };
        _db.Expenses.Add(e);
        await _db.SaveChangesAsync(ct);
        var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == branchId, ct);
        return new ExpenseResponse(e.Id, e.BranchId, e.Category, e.Amount, e.Description, e.ExpenseAt, branch?.Name);
    }

    public async Task<PageResponse<ExpenseResponse>> GetExpensesAsync(Guid branchId, PageRequest r, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default)
    {
        var q = _db.Expenses.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        if (from is not null) q = q.Where(x => x.ExpenseAt >= from);
        if (to is not null) q = q.Where(x => x.ExpenseAt <= to);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderByDescending(x => x.ExpenseAt).Skip((page-1)*size).Take(size)
            .Select(x => new ExpenseResponse(
                x.Id, x.BranchId, x.Category, x.Amount, x.Description, x.ExpenseAt,
                _db.Branches.Where(b => b.Id == x.BranchId).Select(b => b.Name).FirstOrDefault()
            ))
            .ToListAsync(ct);
        return new PageResponse<ExpenseResponse>(items, total, page, size);
    }

    public async Task<WagePayResponse> PayWageAsync(Guid actorUserId, Guid branchId, WagePayRequest r, CancellationToken ct = default)
    {
        if (r.EmployeeUserId == Guid.Empty) throw new ValidationException("Validation failed", new[] { "EmployeeUserId is required." });
        if (r.Amount <= 0) throw new ValidationException("Validation failed", new[] { "Amount must be > 0." });
        if (r.PeriodEnd < r.PeriodStart) throw new ValidationException("Validation failed", new[] { "Invalid period." });

        var wp = new Domain.Entities.WagePayment
        {
            BranchId = branchId,
            EmployeeUserId = r.EmployeeUserId,
            Amount = r.Amount,
            PeriodStart = r.PeriodStart,
            PeriodEnd = r.PeriodEnd,
            PaidAt = DateTimeOffset.UtcNow,
            PaidByUserId = actorUserId,
            Notes = r.Notes?.Trim()
        };
        _db.WagePayments.Add(wp);
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.EmployeeUserId, ct);
        var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == branchId, ct);
        return new WagePayResponse(wp.Id, wp.BranchId, wp.EmployeeUserId, wp.Amount, wp.PeriodStart, wp.PeriodEnd, wp.PaidAt, wp.PaidByUserId, wp.Notes, user?.Email, branch?.Name);
    }

    public async Task<PageResponse<WagePayResponse>> GetWagesAsync(Guid branchId, PageRequest r, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default)
    {
        var q = _db.WagePayments.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        if (from is not null) q = q.Where(x => x.PaidAt >= from);
        if (to is not null) q = q.Where(x => x.PaidAt <= to);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderByDescending(x => x.PaidAt).Skip((page-1)*size).Take(size)
            .Select(x => new WagePayResponse(
                x.Id, x.BranchId, x.EmployeeUserId, x.Amount, x.PeriodStart, x.PeriodEnd, x.PaidAt, x.PaidByUserId, x.Notes,
                _db.Users.Where(u => u.Id == x.EmployeeUserId).Select(u => u.Email).FirstOrDefault(),
                _db.Branches.Where(b => b.Id == x.BranchId).Select(b => b.Name).FirstOrDefault()
            ))
            .ToListAsync(ct);
        return new PageResponse<WagePayResponse>(items, total, page, size);
    }
}
