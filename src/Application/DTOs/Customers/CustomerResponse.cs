namespace Application.DTOs.Customers;
public sealed record CustomerResponse(Guid Id,string FullName,string? Phone,string? Email,string? NationalId);
