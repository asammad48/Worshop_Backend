using Domain.Enums;

namespace Application.DTOs.Customers;

public sealed record CustomerCreateRequest(
    string FullName,
    string? Phone,
    string? Email,
    string? NationalId,
    CustomerType CustomerType = CustomerType.Simple);
