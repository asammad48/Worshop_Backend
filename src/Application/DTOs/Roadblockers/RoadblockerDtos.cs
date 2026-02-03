using Domain.Enums;

namespace Application.DTOs.Roadblockers;

public sealed record RoadblockerCreateRequest(Guid JobCardId, RoadblockerType Type, string? Description);
public sealed record RoadblockerResolveRequest(Guid RoadblockerId);
public sealed record RoadblockerResponse(Guid Id, Guid JobCardId, RoadblockerType Type, string? Description, bool IsResolved, DateTimeOffset CreatedAtLocal, DateTimeOffset? ResolvedAt, Guid CreatedByUserId);
