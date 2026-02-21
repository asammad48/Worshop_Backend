namespace Application.DTOs.Attendance;

public sealed record AttendanceCheckInRequest(
    DateTimeOffset? Timestamp = null,
    string? Note = null
);
