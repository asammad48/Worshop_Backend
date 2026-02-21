namespace Application.DTOs.Attendance;

public sealed record AttendanceCheckOutRequest(
    DateTimeOffset? Timestamp = null,
    string? Note = null
);
