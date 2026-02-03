using Application.DTOs.Reports;

namespace Application.Services.Interfaces;
public interface IReportService
{
    Task<SummaryReportResponse> GetSummaryAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct=default);
    Task<IReadOnlyList<StuckVehicleResponse>> GetStuckVehiclesAsync(Guid branchId, CancellationToken ct=default);
    Task<IReadOnlyList<RoadblockerAgingResponse>> GetRoadblockersAgingAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<IReadOnlyList<StationTimeResponse>> GetStationTimeAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
