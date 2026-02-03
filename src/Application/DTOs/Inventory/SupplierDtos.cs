namespace Application.DTOs.Inventory;
public sealed record SupplierCreateRequest(string Name, string? Phone, string? Email, string? Address);
public sealed record SupplierResponse(Guid Id, string Name, string? Phone, string? Email, string? Address);
