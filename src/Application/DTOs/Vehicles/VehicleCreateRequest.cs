namespace Application.DTOs.Vehicles;
public sealed record VehicleCreateRequest(string Plate,string? Make,string? Model,int? Year,Guid CustomerId);
