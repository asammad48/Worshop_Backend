namespace Domain.Entities;
public sealed class JobCardPartUsage: BaseEntity{ public Guid JobCardId{get;set;} public Guid PartId{get;set;} public Guid LocationId{get;set;} public decimal QuantityUsed{get;set;} public decimal? UnitPrice{get;set;} public DateTimeOffset UsedAt{get;set;} public Guid PerformedByUserId{get;set;} }
