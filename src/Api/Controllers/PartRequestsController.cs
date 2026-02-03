using Api.Security;
using Application.DTOs.JobPartRequests;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Authorize(Policy = "BranchUser")]
public sealed class PartRequestsController : ControllerBase
{
    private readonly IPartRequestService _svc;

    public PartRequestsController(IPartRequestService svc)
    {
        _svc = svc;
    }

    [HttpPost("api/v1/jobcards/{jobCardId}/part-requests")]
    public async Task<ActionResult<ApiResponse<JobPartRequestResponse>>> Create(Guid jobCardId, [FromBody] JobPartRequestCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobPartRequestResponse>.Ok(await _svc.CreateAsync(userId, branchId, jobCardId, req, ct));
    }

    [HttpGet("api/v1/jobcards/{jobCardId}/part-requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobPartRequestResponse>>>> List(Guid jobCardId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<JobPartRequestResponse>>.Ok(await _svc.ListForJobCardAsync(branchId, jobCardId, ct));
    }

    [HttpPost("api/v1/part-requests/{id}/mark-ordered")]
    public async Task<ActionResult<ApiResponse<JobPartRequestResponse>>> MarkOrdered(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobPartRequestResponse>.Ok(await _svc.MarkOrderedAsync(userId, branchId, id, ct));
    }

    [HttpPost("api/v1/part-requests/{id}/mark-arrived")]
    public async Task<ActionResult<ApiResponse<JobPartRequestResponse>>> MarkArrived(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobPartRequestResponse>.Ok(await _svc.MarkArrivedAsync(userId, branchId, id, ct));
    }

    [HttpPost("api/v1/part-requests/{id}/station-sign")]
    public async Task<ActionResult<ApiResponse<JobPartRequestResponse>>> StationSign(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobPartRequestResponse>.Ok(await _svc.StationSignAsync(userId, branchId, id, ct));
    }

    [HttpPost("api/v1/part-requests/{id}/office-sign")]
    public async Task<ActionResult<ApiResponse<JobPartRequestResponse>>> OfficeSign(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobPartRequestResponse>.Ok(await _svc.OfficeSignAsync(userId, branchId, id, ct));
    }
}
