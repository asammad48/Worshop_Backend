using Application.DTOs.Branches;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/branches")]
[Authorize(Policy = "HQOnly")]
public sealed class BranchesController : ControllerBase
{
    private readonly IBranchService _svc;
    public BranchesController(IBranchService svc) { _svc = svc; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> Create([FromBody] BranchCreateRequest req, CancellationToken ct)
        => ApiResponse<BranchResponse>.Ok(await _svc.CreateAsync(req, ct));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<BranchResponse>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
        => ApiResponse<PageResponse<BranchResponse>>.Ok(await _svc.GetPagedAsync(req, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> GetById(Guid id, CancellationToken ct)
        => ApiResponse<BranchResponse>.Ok(await _svc.GetByIdAsync(id, ct));
}
