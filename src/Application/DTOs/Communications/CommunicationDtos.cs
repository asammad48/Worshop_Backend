using Domain.Enums;

namespace Application.DTOs.Communications;

public sealed record CommunicationLogCreateRequest(
    Guid JobCardId,
    CommunicationChannel Channel,
    CommunicationMessageType MessageType,
    string? Notes);

public sealed record CommunicationLogResponse(
    Guid Id,
    Guid BranchId,
    Guid JobCardId,
    CommunicationChannel Channel,
    CommunicationMessageType MessageType,
    DateTimeOffset SentAt,
    string? Notes,
    Guid SentByUserId);
