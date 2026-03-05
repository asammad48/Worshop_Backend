using Domain.Enums;

namespace Application.DTOs.Communications;

public sealed record CommunicationLogCreateRequest(
    Guid JobCardId,
    CommunicationType Type,
    CommunicationDirection Direction,
    string Summary,
    string? Details,
    DateTimeOffset OccurredAt);

public sealed record CommunicationLogResponse(
    Guid Id,
    Guid? BranchId,
    Guid JobCardId,
    CommunicationType Type,
    CommunicationDirection Direction,
    string Summary,
    string? Details,
    DateTimeOffset OccurredAt,
    Guid CreatedByUserId,
    string? CreatedByEmail = null,
    string? JobCardPlate = null);
