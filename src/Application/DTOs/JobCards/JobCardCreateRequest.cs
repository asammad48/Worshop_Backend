namespace Application.DTOs.JobCards;
public sealed record JobCardCreateRequest(Guid VehicleId, int? Mileage, string? InitialReport, DateTimeOffset? RequestedEta = null);
