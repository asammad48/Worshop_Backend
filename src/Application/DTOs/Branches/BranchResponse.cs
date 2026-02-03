namespace Application.DTOs.Branches;
public sealed record BranchResponse(Guid Id,string Name,string? Address,bool IsActive);
