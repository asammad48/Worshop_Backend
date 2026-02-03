namespace Domain.Entities;
public sealed class Location: BaseEntity{ public Guid BranchId{get;set;} public Branch? Branch{get;set;} public string Code{get;set;}=string.Empty; public string Name{get;set;}=string.Empty; public bool IsActive{get;set;}=true; }
