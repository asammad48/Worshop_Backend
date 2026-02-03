namespace Domain.Entities;
public sealed class AuditLog: BaseEntity{ public Guid? BranchId{get;set;} public string Action{get;set;}=string.Empty; public string EntityType{get;set;}=string.Empty; public Guid EntityId{get;set;} public string? OldValue{get;set;} public string? NewValue{get;set;} public Guid PerformedByUserId{get;set;} public DateTimeOffset PerformedAt{get;set;} }
