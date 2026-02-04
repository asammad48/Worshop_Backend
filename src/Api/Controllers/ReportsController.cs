using Api.Security;
using Application.DTOs.Reports;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Policy = "BranchUser")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _svc;
    public ReportsController(IReportService svc) { _svc = svc; }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<SummaryReportResponse>>> Summary([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<SummaryReportResponse>.Ok(await _svc.GetSummaryAsync(branchId, from, to, ct));
    }

    [HttpGet("stuck-vehicles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StuckVehicleResponse>>>> StuckVehicles(CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<StuckVehicleResponse>>.Ok(await _svc.GetStuckVehiclesAsync(branchId, ct));
    }

    [HttpGet("roadblockers-aging")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoadblockerAgingResponse>>>> RoadblockersAging([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<RoadblockerAgingResponse>>.Ok(await _svc.GetRoadblockersAgingAsync(branchId, from, to, ct));
    }

    [HttpGet("station-time")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StationTimeResponse>>>> StationTime([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<StationTimeResponse>>.Ok(await _svc.GetStationTimeAsync(branchId, from, to, ct));
    }
}
