using Domain.Enums;
namespace Domain.Entities;
public sealed class StockTransfer: BaseEntity
{
    public Guid FromBranchId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToBranchId { get; set; }
    public Guid ToLocationId { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;
    public DateTimeOffset? RequestedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string? Notes { get; set; }
}
