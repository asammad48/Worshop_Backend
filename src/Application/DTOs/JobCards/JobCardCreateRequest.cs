namespace Application.DTOs.JobCards;
public sealed record JobCardCreateRequest(Guid VehicleId,int? Mileage,string? InitialReport);
