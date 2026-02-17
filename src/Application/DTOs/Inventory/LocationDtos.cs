namespace Application.DTOs.Inventory;
public sealed record LocationCreateRequest(string Code, string Name);
public sealed record LocationResponse(Guid Id, Guid BranchId, string Code, string Name, bool IsActive, string? BranchName = null);
