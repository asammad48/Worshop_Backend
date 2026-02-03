namespace Application.DTOs.Reports;
public sealed record SummaryReportResponse(decimal TotalExpenses, decimal TotalWages, int VehiclesInShop, IReadOnlyDictionary<string,int> RoadblockersByType);
public sealed record StuckVehicleResponse(Guid JobCardId, Guid VehicleId, DateTimeOffset? EntryAt, string Status);
