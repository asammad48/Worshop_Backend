namespace Application.DTOs.JobCards;

public sealed record JobCardPartUseRequest(Guid LocationId, Guid PartId, decimal QuantityUsed, decimal? UnitPrice, string? Notes);
public sealed record JobCardPartUsageResponse(
    Guid Id,
    Guid JobCardId,
    Guid LocationId,
    Guid PartId,
    decimal QuantityUsed,
    decimal? UnitPrice,
    DateTimeOffset UsedAt,
    Guid PerformedByUserId,
    string? PartSku = null,
    string? PartName = null,
    string? LocationCode = null,
    string? LocationName = null);
