using Domain.Enums;

namespace Application.DTOs.JobTasks;

public sealed record JobTaskResponse(
    Guid Id,
    Guid JobCardId,
    string StationCode,
    string Title,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    Guid? StartedByUserId,
    Guid? EndedByUserId,
    int TotalMinutes,
    string? Notes,
    JobTaskStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
