using Domain.Enums;

namespace Application.DTOs.Billing;

public record JobLineItemCreateRequest(
    JobLineItemType Type,
    string Title,
    decimal Qty,
    decimal UnitPrice,
    string? Notes,
    Guid? PartId,
    Guid? JobPartRequestId
);

public record JobLineItemResponse(
    Guid Id,
    Guid JobCardId,
    JobLineItemType Type,
    string Title,
    decimal Qty,
    decimal UnitPrice,
    decimal Total,
    string? Notes,
    Guid? PartId,
    Guid? JobPartRequestId
);
