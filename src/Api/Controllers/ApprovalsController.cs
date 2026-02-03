using Api.Security;
using Application.DTOs.Approvals;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/approvals")]
[Authorize(Policy = "BranchUser")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IGenericApprovalService _service;

    public ApprovalsController(IGenericApprovalService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ApprovalResponse>>> Create([FromBody] ApprovalCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<ApprovalResponse>.Ok(await _service.CreateAsync(userId, branchId, req, ct));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalResponse>>>> List([FromQuery] string targetType, [FromQuery] Guid targetId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<ApprovalResponse>>.Ok(await _service.ListAsync(branchId, targetType, targetId, ct));
    }
}
