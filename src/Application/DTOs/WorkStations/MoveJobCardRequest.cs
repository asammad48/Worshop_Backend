namespace Application.DTOs.WorkStations;
public sealed record MoveJobCardRequest(Guid WorkStationId,string? Notes);
