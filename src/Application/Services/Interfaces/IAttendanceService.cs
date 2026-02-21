using Application.DTOs.Attendance;
using Application.Pagination;
using Shared.Api;

namespace Application.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceRecordResponse> CheckInAsync(Guid actorUserId, Guid? branchId, AttendanceCheckInRequest request, CancellationToken ct = default);
    Task<AttendanceRecordResponse> CheckOutAsync(Guid actorUserId, Guid? branchId, AttendanceCheckOutRequest request, CancellationToken ct = default);
    Task<AttendanceRecordResponse> UpsertStatusAsync(Guid actorUserId, Guid? branchId, AttendanceUpsertStatusRequest request, CancellationToken ct = default);
    Task<PageResponse<AttendanceRecordResponse>> GetTodayPagedAsync(AttendanceTodayQuery query, CancellationToken ct = default);
    Task<AttendanceMonthResponse> GetEmployeeMonthAsync(AttendanceEmployeeMonthQuery query, CancellationToken ct = default);
}

public record AttendanceTodayQuery(
    Guid? BranchId,
    Guid? EmployeeUserId,
    int PageNumber = 1,
    int PageSize = 50
);

public record AttendanceEmployeeMonthQuery(
    Guid EmployeeUserId,
    int Year,
    int Month,
    Guid? BranchId
);
