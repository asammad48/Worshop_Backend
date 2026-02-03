namespace Domain.Entities;
public sealed class StockAdjustment: BaseEntity{ public Guid BranchId{get;set;} public Guid LocationId{get;set;} public Guid PartId{get;set;} public decimal QuantityDelta{get;set;} public string Reason{get;set;}=string.Empty; public Guid CreatedByUserId{get;set;} public Guid? ApprovedByUserId{get;set;} }
