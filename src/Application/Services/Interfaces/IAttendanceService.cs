using Application.DTOs.Attendance;

namespace Application.Services.Interfaces;

public interface IAttendanceService
{
    Task CheckInAsync(Guid userId, Guid? branchId, AttendanceCheckInRequest req, CancellationToken ct = default);
    Task CheckOutAsync(Guid userId, AttendanceCheckOutRequest req, CancellationToken ct = default);
    Task<AttendanceMonthResponse> GetMyMonthAsync(Guid userId, int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceDaySummaryResponse>> GetTodayAttendanceAsync(Guid? branchId, CancellationToken ct = default);
    Task<AttendanceMonthResponse> GetUserMonthAsync(Guid userId, int year, int month, CancellationToken ct = default);
}
