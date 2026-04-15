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

public sealed record JobCardReportResponse(
    IReadOnlyList<JobCardReportItemResponse> Items,
    int TotalCount,
    DateTimeOffset From,
    DateTimeOffset To);

public sealed record JobCardReportItemResponse(
    Guid JobCardId,
    DateTimeOffset CreatedAt,
    string Status,
    DateTimeOffset? EntryAt,
    DateTimeOffset? ExitAt,
    int? Mileage,
    Guid CustomerId,
    string? CustomerName,
    string CustomerType,
    Guid VehicleId,
    string? VehiclePlate,
    string? VehicleMake,
    string? VehicleModel,
    int? VehicleYear,
    string? InitialReport,
    string? Diagnosis,
    string? LatestDiagnosisSummary,
    DateTimeOffset? RequestedEta,
    DateTimeOffset? LatestEstimatedEta,
    decimal? LatestEstimatedPrice,
    string? CurrentStationCode,
    string? CurrentStationName,
    DateTimeOffset? LastStationMovedAt,
    int StationMovementCount,
    int DiagnosisLogCount,
    decimal? InvoiceTotal,
    string? InvoicePaymentStatus);
