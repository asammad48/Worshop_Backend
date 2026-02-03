namespace Application.DTOs.Inventory;
public sealed record PartCreateRequest(string Sku, string Name, string? Brand, string? Unit);
public sealed record PartResponse(Guid Id, string Sku, string Name, string? Brand, string? Unit);
