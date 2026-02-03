using Api.Security;
using Application.DTOs.Roadblockers;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/roadblockers")]
[Authorize(Policy = "BranchUser")]
public sealed class RoadblockersController : ControllerBase
{
    private readonly IRoadblockerService _svc;
    public RoadblockersController(IRoadblockerService svc) { _svc = svc; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoadblockerResponse>>> Create([FromBody] RoadblockerCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<RoadblockerResponse>.Ok(await _svc.CreateAsync(userId, branchId, req, ct));
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<ApiResponse<RoadblockerResponse>>> Resolve(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<RoadblockerResponse>.Ok(await _svc.ResolveAsync(userId, branchId, id, ct));
    }

    [HttpGet("jobcard/{jobCardId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoadblockerResponse>>>> ListByJobCard(Guid jobCardId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<RoadblockerResponse>>.Ok(await _svc.ListByJobCardAsync(branchId, jobCardId, ct));
    }
}
