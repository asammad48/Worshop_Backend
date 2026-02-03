namespace Application.DTOs.Inventory;
public sealed record StockItemResponse(Guid PartId, Guid LocationId, decimal QuantityOnHand);
public sealed record StockAdjustRequest(Guid LocationId, Guid PartId, decimal QuantityDelta, string Reason);
