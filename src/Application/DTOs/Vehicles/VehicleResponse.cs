namespace Application.DTOs.Vehicles;
public sealed record VehicleResponse(Guid Id,string Plate,string? Make,string? Model,int? Year,Guid CustomerId);
