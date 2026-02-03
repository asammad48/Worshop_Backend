using Api.Security;
using Application.DTOs.Inventory;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/purchase-orders")]
[Authorize(Policy = "BranchUser")]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _svc;
    public PurchaseOrdersController(IPurchaseOrderService svc) { _svc = svc; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseOrderResponse>>> Create([FromBody] PurchaseOrderCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<PurchaseOrderResponse>.Ok(await _svc.CreateAsync(userId, branchId, req, ct));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderResponse>>> Submit(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<PurchaseOrderResponse>.Ok(await _svc.SubmitAsync(userId, branchId, id, ct));
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderResponse>>> Receive(Guid id, [FromBody] PurchaseOrderReceiveRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<PurchaseOrderResponse>.Ok(await _svc.ReceiveAsync(userId, branchId, id, req, ct));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<PurchaseOrderResponse>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<PurchaseOrderResponse>>.Ok(await _svc.GetPagedAsync(branchId, req, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PurchaseOrderResponse>.Ok(await _svc.GetByIdAsync(branchId, id, ct));
    }
}
