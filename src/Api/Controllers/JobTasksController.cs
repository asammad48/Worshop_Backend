using Api.Security;
using Application.DTOs.JobTasks;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/jobtasks")]
[Authorize(Policy = "BranchUser")]
public sealed class JobTasksController : ControllerBase
{
    private readonly IJobTaskService _service;

    public JobTasksController(IJobTaskService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobTaskResponse>>> Create([FromBody] JobTaskCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobTaskResponse>.Ok(await _service.CreateAsync(userId, branchId, req, ct));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ApiResponse<JobTaskResponse>>> Start(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobTaskResponse>.Ok(await _service.StartAsync(userId, branchId, id, ct));
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<ActionResult<ApiResponse<JobTaskResponse>>> Stop(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobTaskResponse>.Ok(await _service.StopAsync(userId, branchId, id, ct));
    }

    [HttpGet("/api/v1/jobcards/{jobCardId:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobTaskResponse>>>> ListByJobCard(Guid jobCardId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<JobTaskResponse>>.Ok(await _service.ListByJobCardAsync(branchId, jobCardId, ct));
    }
}
