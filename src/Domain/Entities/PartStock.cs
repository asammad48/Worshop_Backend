namespace Domain.Entities;
public sealed class PartStock: BaseEntity{ public Guid BranchId{get;set;} public Guid LocationId{get;set;} public Location? Location{get;set;} public Guid PartId{get;set;} public Part? Part{get;set;} public decimal QuantityOnHand{get;set;} }
