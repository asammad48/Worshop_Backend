using Api.Security;
using Application.DTOs.Inventory;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/transfers")]
[Authorize(Policy = "BranchUser")]
public sealed class TransfersController : ControllerBase
{
    private readonly ITransferService _svc;
    public TransfersController(ITransferService svc) { _svc = svc; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TransferResponse>>> Create([FromBody] TransferCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<TransferResponse>.Ok(await _svc.CreateAsync(userId, branchId, req, ct));
    }

    [HttpPost("{id:guid}/request")]
    public async Task<ActionResult<ApiResponse<TransferResponse>>> Request(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<TransferResponse>.Ok(await _svc.RequestAsync(userId, branchId, id, ct));
    }

    [HttpPost("{id:guid}/ship")]
    public async Task<ActionResult<ApiResponse<TransferResponse>>> Ship(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<TransferResponse>.Ok(await _svc.ShipAsync(userId, branchId, id, ct));
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<TransferResponse>>> Receive(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<TransferResponse>.Ok(await _svc.ReceiveAsync(userId, branchId, id, ct));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<TransferResponse>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<TransferResponse>>.Ok(await _svc.GetPagedAsync(branchId, req, ct));
    }
}
