using Api.Security;
using Application.DTOs.Communications;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/communications")]
[Authorize(Policy = "BranchUser")]
public sealed class CommunicationsController : ControllerBase
{
    private readonly ICommunicationService _service;

    public CommunicationsController(ICommunicationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CommunicationLogResponse>>> Create([FromBody] CommunicationLogCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<CommunicationLogResponse>.Ok(await _service.CreateAsync(userId, branchId, req, ct));
    }

    [HttpGet("/api/v1/jobcards/{jobCardId:guid}/communications")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommunicationLogResponse>>>> ListByJobCard(Guid jobCardId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<CommunicationLogResponse>>.Ok(await _service.ListByJobCardAsync(branchId, jobCardId, ct));
    }
}
