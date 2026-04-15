namespace Application.DTOs.Drivers;

public sealed record DriverResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string FullName,
    string? Phone,
    string? LicenseNumber);
