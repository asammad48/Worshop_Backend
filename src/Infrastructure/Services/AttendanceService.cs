using Application.DTOs.Attendance;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Shared.Api;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _repo;
    private readonly IUserService _userService;

    public AttendanceService(IAttendanceRepository repo, IUserService userService)
    {
        _repo = repo;
        _userService = userService;
    }

    public async Task<AttendanceRecordResponse> CheckInAsync(Guid actorUserId, Guid? branchId, AttendanceCheckInRequest request, CancellationToken ct = default)
    {
        var targetUserId = request.EmployeeUserId ?? actorUserId;
        var workDate = request.WorkDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (targetUserId != actorUserId)
        {
            await EnsureAdminOrManager(actorUserId, branchId);
        }

        var existing = await _repo.GetByEmployeeAndDateAsync(targetUserId, workDate, ct);
        if (existing != null && existing.CheckInAt != null)
        {
            return Map(existing);
        }

        var record = existing ?? new AttendanceRecord
        {
            EmployeeUserId = targetUserId,
            WorkDate = workDate,
            BranchId = branchId,
            Status = AttendanceStatus.Present,
            Source = targetUserId == actorUserId ? AttendanceSource.Self : AttendanceSource.Admin,
            Note = request.Note,
            CheckInAt = DateTimeOffset.UtcNow
        };

        if (existing != null)
        {
            record.CheckInAt = DateTimeOffset.UtcNow;
            record.Note = request.Note ?? record.Note;
        }

        var saved = await _repo.UpsertAsync(record, ct);
        return Map(saved);
    }

    public async Task<AttendanceRecordResponse> CheckOutAsync(Guid actorUserId, Guid? branchId, AttendanceCheckOutRequest request, CancellationToken ct = default)
    {
        var targetUserId = request.EmployeeUserId ?? actorUserId;
        var workDate = request.WorkDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (targetUserId != actorUserId)
        {
            await EnsureAdminOrManager(actorUserId, branchId);
        }

        var record = await _repo.GetByEmployeeAndDateAsync(targetUserId, workDate, ct);
        if (record == null || record.CheckInAt == null)
        {
            throw new ValidationException("Cannot check out without check-in", new[] { "Check-in record not found for this date." });
        }

        if (record.CheckOutAt != null)
        {
            return Map(record);
        }

        record.CheckOutAt = DateTimeOffset.UtcNow;
        if (request.Note != null) record.Note = request.Note;

        if (record.CheckOutAt < record.CheckInAt)
        {
             throw new ValidationException("Checkout must be after checkin", new[] { "CheckOutAt < CheckInAt" });
        }

        var saved = await _repo.UpsertAsync(record, ct);
        return Map(saved);
    }

    public async Task<AttendanceRecordResponse> UpsertStatusAsync(Guid actorUserId, Guid? branchId, AttendanceUpsertStatusRequest request, CancellationToken ct = default)
    {
        await EnsureAdminOrManager(actorUserId, branchId);

        var record = await _repo.GetByEmployeeAndDateAsync(request.EmployeeUserId, request.WorkDate, ct) ?? new AttendanceRecord
        {
            EmployeeUserId = request.EmployeeUserId,
            WorkDate = request.WorkDate,
            BranchId = branchId
        };

        record.Status = request.Status;
        record.CheckInAt = request.CheckInAt;
        record.CheckOutAt = request.CheckOutAt;
        record.Note = request.Note;
        record.Source = AttendanceSource.Admin;

        if (record.CheckInAt != null && record.CheckOutAt != null && record.CheckOutAt < record.CheckInAt)
        {
            throw new ValidationException("Checkout must be after checkin", new[] { "CheckOutAt < CheckInAt" });
        }

        var saved = await _repo.UpsertAsync(record, ct);
        return Map(saved);
    }

    public async Task<PageResponse<AttendanceRecordResponse>> GetTodayPagedAsync(AttendanceTodayQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _repo.GetTodayPagedAsync(query.BranchId, query.EmployeeUserId, query.PageNumber, query.PageSize, ct);
        var mapped = items.Select(Map).ToList();
        return new PageResponse<AttendanceRecordResponse>(mapped, total, query.PageNumber, query.PageSize);
    }

    public async Task<AttendanceMonthResponse> GetEmployeeMonthAsync(AttendanceEmployeeMonthQuery query, CancellationToken ct = default)
    {
        var start = new DateOnly(query.Year, query.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var items = await _repo.GetEmployeeMonthRangeAsync(query.EmployeeUserId, query.BranchId, start, end, ct);

        var mapped = items.Select(Map).ToList();
        return new AttendanceMonthResponse(query.EmployeeUserId, query.Year, query.Month, mapped);
    }

    private async Task EnsureAdminOrManager(Guid actorUserId, Guid? branchId)
    {
        var user = await _userService.GetByIdAsync(actorUserId);
        if (user.Role == UserRole.HQ_ADMIN) return;
        if (user.Role == UserRole.BRANCH_MANAGER && user.BranchId == branchId) return;

        throw new ForbiddenException("Only Admin or Branch Manager can perform this action.");
    }

    private static AttendanceRecordResponse Map(AttendanceRecord x) => new(
        x.Id,
        x.BranchId,
        x.EmployeeUserId,
        x.EmployeeUser?.Email ?? "unknown",
        x.WorkDate,
        x.CheckInAt,
        x.CheckOutAt,
        x.Status,
        x.Source,
        x.Note
    );
}
