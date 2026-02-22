namespace Application.DTOs.Dashboard;

public sealed record InventoryDashboardResponse(
    Guid? BranchId,
    int LowStockCount,
    int PendingPoCount,
    int PendingTransfersCount,
    IReadOnlyList<LowStockRow> LowStockTop,
    IReadOnlyList<PurchaseOrderRow> PendingPoTop,
    IReadOnlyList<TransferRow> PendingTransfersTop
);

public sealed record LowStockRow(
    Guid PartId,
    string PartSku,
    string PartName,
    string LocationName,
    decimal QuantityOnHand,
    decimal ReorderLevel
);

public sealed record PurchaseOrderRow(
    Guid PurchaseOrderId,
    string OrderNo,
    string SupplierName,
    string Status,
    DateTimeOffset OrderedAt,
    int DaysPending
);

public sealed record TransferRow(
    Guid TransferId,
    string TransferNo,
    string Status,
    string FromBranch,
    string ToBranch,
    DateTimeOffset RequestedAt,
    int DaysPending
);
