namespace Application.DTOs.JobCards;

public sealed record JobCardDiagnosisLogCreateRequest(
    string DiagnosisNote,
    DateTimeOffset? EstimatedEta,
    decimal? EstimatedPrice);
