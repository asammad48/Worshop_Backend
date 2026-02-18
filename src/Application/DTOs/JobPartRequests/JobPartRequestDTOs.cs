using Domain.Enums;

namespace Application.DTOs.JobPartRequests;

public record JobPartRequestCreateRequest(Guid PartId, decimal Qty, string StationCode);

public record JobPartRequestResponse(
    Guid Id,
    Guid BranchId,
    Guid JobCardId,
    Guid PartId,
    decimal Qty,
    string StationCode,
    DateTimeOffset RequestedAt,
    DateTimeOffset? OrderedAt,
    DateTimeOffset? ArrivedAt,
    Guid? StationSignedByUserId,
    Guid? OfficeSignedByUserId,
    JobPartRequestStatus Status,
    Guid? SupplierId,
    Guid? PurchaseOrderId,
    string? PartSku = null,
    string? PartName = null,
    string? SupplierName = null,
    string? RequestedByEmail = null,
    string? WorkStationName = null
);
