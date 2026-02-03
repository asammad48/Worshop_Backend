namespace Application.DTOs.JobTasks;

public sealed record JobTaskCreateRequest(
    Guid JobCardId,
    string StationCode,
    string Title,
    string? Notes);
