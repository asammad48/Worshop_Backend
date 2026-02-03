namespace Application.DTOs.Customers;
public sealed record CustomerCreateRequest(string FullName,string? Phone,string? Email,string? NationalId);
