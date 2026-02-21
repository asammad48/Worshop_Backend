namespace Application.DTOs.Attendance;

public sealed record AttendanceDaySummaryResponse(
    Guid UserId,
    DateTime Date,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt,
    int TotalMinutes,
    string? Notes
);
