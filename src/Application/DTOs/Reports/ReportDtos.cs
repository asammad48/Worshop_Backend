namespace Application.DTOs.Reports;
public sealed record SummaryReportResponse(
    decimal TotalExpenses,
    decimal TotalWages,
    int VehiclesInShop,
    IReadOnlyDictionary<string,int> RoadblockersByType,
    int CommunicationsCount);
public sealed record StuckVehicleResponse(Guid JobCardId, Guid VehicleId, DateTimeOffset? EntryAt, string Status);

public sealed record RoadblockerAgingResponse(
    Guid RoadblockerId,
    Guid JobCardId,
    string Type,
    string Description,
    DateTimeOffset CreatedAt,
    int DaysOpen);

public sealed record StationTimeResponse(
    string StationCode,
    int Year,
    int WeekNumber,
    int TotalMinutes);
