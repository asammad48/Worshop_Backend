namespace Application.DTOs.WorkStations;
public sealed record JobCardStationHistoryResponse(Guid Id,Guid JobCardId,Guid WorkStationId,DateTimeOffset MovedAt,Guid MovedByUserId,string? Notes);
