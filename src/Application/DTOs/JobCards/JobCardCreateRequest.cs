namespace Application.DTOs.JobCards;
public sealed record JobCardCreateRequest(Guid VehicleId, Guid? DriverId, int? Mileage, string? InitialReport, DateTimeOffset? RequestedEta = null);
