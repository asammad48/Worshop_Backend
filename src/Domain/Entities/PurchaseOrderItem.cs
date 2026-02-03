namespace Domain.Entities;
public sealed class PurchaseOrderItem: BaseEntity{ public Guid PurchaseOrderId{get;set;} public Guid PartId{get;set;} public decimal Qty{get;set;} public decimal UnitCost{get;set;} public decimal ReceivedQty{get;set;} }
