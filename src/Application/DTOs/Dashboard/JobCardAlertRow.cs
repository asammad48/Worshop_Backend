namespace Application.DTOs.Dashboard;

public sealed record JobCardAlertRow(
    Guid JobCardId,
    string Plate,
    string CustomerName,
    string Status,
    DateTimeOffset? EntryAt,
    DateTimeOffset? ExitAt,
    int DaysInShop,
    bool HasRoadblocker,
    string? RoadblockerSummary,
    bool RequiresApproval,
    string? RequiredApprovalRole,
    decimal? EstimatedTotal,
    decimal? DueAmount
);
