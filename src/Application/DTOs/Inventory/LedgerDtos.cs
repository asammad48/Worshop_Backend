using Domain.Enums;
namespace Application.DTOs.Inventory;
public sealed record LedgerRowResponse(
    Guid Id,
    Guid BranchId,
    Guid LocationId,
    Guid PartId,
    StockMovementType MovementType,
    string ReferenceType,
    Guid? ReferenceId,
    decimal QuantityDelta,
    decimal? UnitCost,
    string? Notes,
    Guid PerformedByUserId,
    DateTimeOffset PerformedAt,
    string? PartSku = null,
    string? PartName = null,
    string? LocationCode = null,
    string? LocationName = null,
    string? PerformedByEmail = null
);
