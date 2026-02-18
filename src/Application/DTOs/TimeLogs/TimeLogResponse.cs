namespace Application.DTOs.TimeLogs;
public sealed record TimeLogResponse(
    Guid Id,
    Guid JobCardId,
    Guid TechnicianUserId,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int TotalMinutes,
    string? UserEmail = null,
    string? TaskTitle = null,
    string? WorkStationName = null);
