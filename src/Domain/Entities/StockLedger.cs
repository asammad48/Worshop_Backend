using Domain.Enums;
namespace Domain.Entities;
public sealed class StockLedger: BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid LocationId { get; set; }
    public Guid PartId { get; set; }
    public StockMovementType MovementType { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
    public Guid PerformedByUserId { get; set; }
    public DateTimeOffset PerformedAt { get; set; }
}
