using Domain.Entities;
using Application.DTOs.Attendance;

namespace Application.Services.Interfaces;

public interface IAttendanceRepository
{
    Task<AttendanceRecord?> GetByEmployeeAndDateAsync(Guid employeeUserId, DateOnly workDate, CancellationToken ct = default);
    Task<AttendanceRecord> UpsertAsync(AttendanceRecord record, CancellationToken ct = default);
    Task<(IReadOnlyList<AttendanceRecord> Items, int Total)> GetTodayPagedAsync(Guid? branchId, Guid? employeeUserId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecord>> GetEmployeeMonthRangeAsync(Guid employeeUserId, Guid? branchId, DateOnly start, DateOnly end, CancellationToken ct = default);
}
