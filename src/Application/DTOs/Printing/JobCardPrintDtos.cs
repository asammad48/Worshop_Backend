namespace Application.DTOs.Printing;

public sealed record JobCardPrintHeaderDto(
    Guid JobCardId,
    string JobCardNo,
    string Plate,
    string CustomerName,
    string? CustomerPhone,
    string BranchName,
    DateTimeOffset EntryAt,
    DateTimeOffset? ExitAt,
    int DaysInShop,
    string Status,
    string? Notes);

public sealed record JobCardPrintTaskDto(
    Guid TaskId,
    string Title,
    string Status,
    string? AssignedToEmail,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Notes);

public sealed record JobCardPrintPartDto(
    string PartSku,
    string PartName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? IssuedFromLocation,
    DateTimeOffset IssuedAt);

public sealed record JobCardPrintPartRequestDto(
    string PartSku,
    string PartName,
    decimal QuantityRequested,
    string Status,
    string RequestedByEmail,
    DateTimeOffset RequestedAt,
    string? SupplierName,
    string? Note);

public sealed record JobCardPrintRoadblockerDto(
    string Title,
    string Status,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    string? ResolvedByEmail,
    DateTimeOffset? ResolvedAt,
    string? Note);

public sealed record JobCardPrintTimeLogDto(
    string UserEmail,
    string? TaskTitle,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int DurationMinutes);

public sealed record JobCardPrintCommunicationDto(
    string Type,
    string Direction,
    string Summary,
    string? Details,
    DateTimeOffset OccurredAt,
    string CreatedByEmail);

public sealed record JobCardPrintFinancialDto(
    bool HasInvoice,
    string? InvoiceNo,
    decimal Subtotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    decimal Paid,
    decimal Due);

public sealed record JobCardPrintResponse(
    JobCardPrintHeaderDto Header,
    IReadOnlyList<JobCardPrintTaskDto> Tasks,
    IReadOnlyList<JobCardPrintPartDto> PartsUsed,
    IReadOnlyList<JobCardPrintPartRequestDto> PartRequests,
    IReadOnlyList<JobCardPrintRoadblockerDto> Roadblockers,
    IReadOnlyList<JobCardPrintTimeLogDto> TimeLogs,
    IReadOnlyList<JobCardPrintCommunicationDto> Communications,
    JobCardPrintFinancialDto Financial);
