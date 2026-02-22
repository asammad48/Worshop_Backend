using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _db;

    public AttendanceRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AttendanceRecord?> GetByEmployeeAndDateAsync(Guid employeeUserId, DateOnly workDate, CancellationToken ct = default)
    {
        return await _db.AttendanceRecords
            .Include(x => x.EmployeeUser)
            .FirstOrDefaultAsync(x => x.EmployeeUserId == employeeUserId && x.WorkDate == workDate, ct);
    }

    public async Task<AttendanceRecord> UpsertAsync(AttendanceRecord record, CancellationToken ct = default)
    {
        var existing = await _db.AttendanceRecords
            .FirstOrDefaultAsync(x => x.EmployeeUserId == record.EmployeeUserId && x.WorkDate == record.WorkDate, ct);

        if (existing == null)
        {
            _db.AttendanceRecords.Add(record);
        }
        else
        {
            existing.CheckInAt = record.CheckInAt ?? existing.CheckInAt;
            existing.CheckOutAt = record.CheckOutAt ?? existing.CheckOutAt;
            existing.Status = record.Status;
            existing.Source = record.Source;
            existing.Note = record.Note ?? existing.Note;
            existing.BranchId = record.BranchId ?? existing.BranchId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            // record = existing; // Not needed as we return existing
        }

        await _db.SaveChangesAsync(ct);

        // Return enriched
        return await _db.AttendanceRecords
            .Include(x => x.EmployeeUser)
            .FirstAsync(x => x.EmployeeUserId == record.EmployeeUserId && x.WorkDate == record.WorkDate, ct);
    }

    public async Task<(IReadOnlyList<AttendanceRecord> Items, int Total)> GetTodayPagedAsync(Guid? branchId, Guid? employeeUserId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.AttendanceRecords
            .Include(x => x.EmployeeUser)
            .Where(x => x.WorkDate == today);

        if (branchId.HasValue)
        {
            query = query.Where(x => x.BranchId == branchId.Value);
        }

        if (employeeUserId.HasValue)
        {
            query = query.Where(x => x.EmployeeUserId == employeeUserId.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetEmployeeMonthRangeAsync(Guid employeeUserId, Guid? branchId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var query = _db.AttendanceRecords
            .Include(x => x.EmployeeUser)
            .Where(x => x.EmployeeUserId == employeeUserId && x.WorkDate >= start && x.WorkDate <= end);

        if (branchId.HasValue)
        {
            query = query.Where(x => x.BranchId == branchId.Value);
        }

        return await query
            .OrderBy(x => x.WorkDate)
            .ToListAsync(ct);
    }
}
