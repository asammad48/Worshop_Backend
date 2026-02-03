using Api.Security;
using Application.DTOs.JobCards;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/jobcards/{jobCardId:guid}/parts")]
[Authorize(Policy = "BranchUser")]
public sealed class JobCardPartsController : ControllerBase
{
    private readonly IJobCardPartsService _svc;
    public JobCardPartsController(IJobCardPartsService svc) { _svc = svc; }

    [HttpPost("use")]
    public async Task<ActionResult<ApiResponse<JobCardPartUsageResponse>>> Use(Guid jobCardId, [FromBody] JobCardPartUseRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardPartUsageResponse>.Ok(await _svc.UsePartAsync(userId, branchId, jobCardId, req, ct));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobCardPartUsageResponse>>>> List(Guid jobCardId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<JobCardPartUsageResponse>>.Ok(await _svc.ListAsync(branchId, jobCardId, ct));
    }
}
