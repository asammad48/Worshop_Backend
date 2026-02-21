using Domain.Enums;

namespace Application.DTOs.Attendance;

public sealed record AttendanceCheckInRequest(
    Guid? EmployeeUserId,
    DateOnly? WorkDate,
    string? Note
);

public sealed record AttendanceCheckOutRequest(
    Guid? EmployeeUserId,
    DateOnly? WorkDate,
    string? Note
);

public sealed record AttendanceUpsertStatusRequest(
    Guid EmployeeUserId,
    DateOnly WorkDate,
    AttendanceStatus Status,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt,
    string? Note
);

public sealed record AttendanceRecordResponse(
    Guid Id,
    Guid? BranchId,
    Guid EmployeeUserId,
    string EmployeeEmail,
    DateOnly WorkDate,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt,
    AttendanceStatus Status,
    AttendanceSource Source,
    string? Note
);

public sealed record AttendanceMonthResponse(
    Guid EmployeeUserId,
    int Year,
    int Month,
    IReadOnlyList<AttendanceRecordResponse> Days
);
