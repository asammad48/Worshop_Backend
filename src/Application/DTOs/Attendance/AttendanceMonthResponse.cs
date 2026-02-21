namespace Application.DTOs.Attendance;

public sealed record AttendanceMonthResponse(
    Guid UserId,
    int Month,
    int Year,
    IReadOnlyList<AttendanceDaySummaryResponse> Days,
    int TotalMinutes,
    decimal TotalHours
);
