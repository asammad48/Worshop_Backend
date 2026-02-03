namespace Domain.Entities;
public sealed class StockTransferItem: BaseEntity{ public Guid StockTransferId{get;set;} public Guid PartId{get;set;} public decimal Qty{get;set;} }
