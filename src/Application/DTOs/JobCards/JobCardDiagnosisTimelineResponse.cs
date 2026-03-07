namespace Application.DTOs.JobCards;

public sealed record JobCardDiagnosisTimelineResponse(
    Guid JobCardId,
    DateTimeOffset? RequestedEta,
    DateTimeOffset? LatestEstimatedEta,
    decimal? LatestEstimatedPrice,
    string? LatestDiagnosisSummary,
    IReadOnlyList<JobCardDiagnosisLogResponse> Logs);
