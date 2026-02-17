namespace Application.DTOs.WorkStations;
public sealed record WorkStationResponse(Guid Id,Guid BranchId,string Code,string Name,bool IsActive,string? BranchName = null);
