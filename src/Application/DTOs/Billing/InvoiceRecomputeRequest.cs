namespace Application.DTOs.Billing;

public sealed record InvoiceRecomputeRequest(
    Guid JobCardId,
    string? Reason
);
