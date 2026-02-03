using Api.Security;
using Application.DTOs.WorkStations;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/workstations")]
[Authorize(Policy = "BranchUser")]
public sealed class WorkStationsController : ControllerBase
{
    private readonly IWorkStationService _svc;
    public WorkStationsController(IWorkStationService svc) { _svc = svc; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkStationResponse>>> Create([FromBody] WorkStationCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<WorkStationResponse>.Ok(await _svc.CreateAsync(branchId, req, ct));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<WorkStationResponse>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<WorkStationResponse>>.Ok(await _svc.GetPagedAsync(branchId, req, ct));
    }
}
