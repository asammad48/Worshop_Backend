namespace Application.DTOs.Inventory;
public sealed record StockItemResponse(
    Guid PartId,
    Guid LocationId,
    decimal QuantityOnHand,
    string? PartSku = null,
    string? PartName = null,
    string? LocationCode = null,
    string? LocationName = null
);
public sealed record StockAdjustRequest(Guid LocationId, Guid PartId, decimal QuantityDelta, string Reason);
