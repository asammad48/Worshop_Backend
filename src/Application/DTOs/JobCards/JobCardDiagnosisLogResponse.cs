namespace Application.DTOs.JobCards;

public sealed record JobCardDiagnosisLogResponse(
    Guid Id,
    Guid JobCardId,
    string DiagnosisNote,
    DateTimeOffset? EstimatedEta,
    decimal? EstimatedPrice,
    Guid CreatedByUserId,
    string CreatedByEmail,
    DateTimeOffset CreatedAt);
