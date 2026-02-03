using Domain.Enums;
namespace Application.DTOs.Inventory;
public sealed record TransferItemCreate(Guid PartId, decimal Qty);
public sealed record TransferCreateRequest(Guid FromLocationId, Guid ToBranchId, Guid ToLocationId, string? Notes, IReadOnlyList<TransferItemCreate> Items);
public sealed record TransferResponse(Guid Id, string TransferNo, StockTransferStatus Status, Guid FromBranchId, Guid FromLocationId, Guid ToBranchId, Guid ToLocationId, DateTimeOffset? RequestedAt, DateTimeOffset? ShippedAt, DateTimeOffset? ReceivedAt, string? Notes);
