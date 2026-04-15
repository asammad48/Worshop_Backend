namespace Application.DTOs.Drivers;

public sealed record DriverCreateRequest(
    Guid CustomerId,
    string FullName,
    string? Phone,
    string? LicenseNumber);
