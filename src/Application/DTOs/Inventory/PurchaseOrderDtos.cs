using Domain.Enums;
namespace Application.DTOs.Inventory;
public sealed record PurchaseOrderItemCreate(Guid PartId, decimal Qty, decimal UnitCost);
public sealed record PurchaseOrderCreateRequest(Guid SupplierId, string? Notes, IReadOnlyList<PurchaseOrderItemCreate> Items, IReadOnlyList<Guid>? JobPartRequestIds = null);
public sealed record PurchaseOrderResponse(Guid Id, Guid BranchId, Guid SupplierId, string OrderNo, PurchaseOrderStatus Status, DateTimeOffset? OrderedAt, DateTimeOffset? ReceivedAt, string? Notes);
public sealed record ReceiveItem(Guid PartId, decimal ReceiveQty, decimal UnitCost);
public sealed record PurchaseOrderReceiveRequest(Guid LocationId, IReadOnlyList<ReceiveItem> Items);
