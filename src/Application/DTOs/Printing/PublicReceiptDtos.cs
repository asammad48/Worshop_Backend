namespace Application.DTOs.Printing;

public sealed record PublicReceiptInvoiceLineDto(
    string Name,
    decimal Qty,
    decimal UnitPrice,
    decimal Amount);

public sealed record PublicReceiptInvoiceDto(
    bool HasInvoice,
    decimal Subtotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    decimal Paid,
    decimal Due,
    IReadOnlyList<PublicReceiptInvoiceLineDto> Lines);

public sealed record PublicReceiptPaymentDto(
    DateTimeOffset PaidAt,
    decimal Amount,
    string Method);

public sealed record PublicReceiptCommDto(
    DateTimeOffset OccurredAt,
    string Summary);

public sealed record PublicJobCardReceiptResponse(
    Guid JobCardId,
    string Plate,
    string CustomerName,
    string BranchName,
    DateTimeOffset EntryAt,
    DateTimeOffset? ExitAt,
    DateTimeOffset? RequestedEta,
    DateTimeOffset? LatestEstimatedEta,
    string Status,
    PublicReceiptInvoiceDto Invoice,
    IReadOnlyList<PublicReceiptPaymentDto> Payments,
    IReadOnlyList<PublicReceiptCommDto> Communications);
