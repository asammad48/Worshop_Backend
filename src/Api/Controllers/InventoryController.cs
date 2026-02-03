using Api.Security;
using Application.DTOs.Inventory;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize(Policy = "BranchUser")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inv;
    public InventoryController(IInventoryService inv) { _inv = inv; }

    [HttpPost("suppliers")]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> CreateSupplier([FromBody] SupplierCreateRequest req, CancellationToken ct)
        => ApiResponse<SupplierResponse>.Ok(await _inv.CreateSupplierAsync(req, ct));

    [HttpGet("suppliers")]
    public async Task<ActionResult<ApiResponse<PageResponse<SupplierResponse>>>> GetSuppliers([FromQuery] PageRequest req, CancellationToken ct)
        => ApiResponse<PageResponse<SupplierResponse>>.Ok(await _inv.GetSuppliersAsync(req, ct));

    [HttpPost("parts")]
    public async Task<ActionResult<ApiResponse<PartResponse>>> CreatePart([FromBody] PartCreateRequest req, CancellationToken ct)
        => ApiResponse<PartResponse>.Ok(await _inv.CreatePartAsync(req, ct));

    [HttpGet("parts")]
    public async Task<ActionResult<ApiResponse<PageResponse<PartResponse>>>> GetParts([FromQuery] PageRequest req, CancellationToken ct)
        => ApiResponse<PageResponse<PartResponse>>.Ok(await _inv.GetPartsAsync(req, ct));

    [HttpPost("locations")]
    public async Task<ActionResult<ApiResponse<LocationResponse>>> CreateLocation([FromBody] LocationCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<LocationResponse>.Ok(await _inv.CreateLocationAsync(branchId, req, ct));
    }

    [HttpGet("locations")]
    public async Task<ActionResult<ApiResponse<PageResponse<LocationResponse>>>> GetLocations([FromQuery] PageRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<LocationResponse>>.Ok(await _inv.GetLocationsAsync(branchId, req, ct));
    }

    [HttpGet("stock")]
    public async Task<ActionResult<ApiResponse<PageResponse<StockItemResponse>>>> GetStock([FromQuery] PageRequest req, [FromQuery] Guid? locationId, [FromQuery] Guid? partId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<StockItemResponse>>.Ok(await _inv.GetStockAsync(branchId, req, locationId, partId, ct));
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<ApiResponse<string>>> Adjust([FromBody] StockAdjustRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        await _inv.AdjustStockAsync(userId, branchId, req, ct);
        return ApiResponse<string>.Ok("OK");
    }

    [HttpGet("ledger")]
    public async Task<ActionResult<ApiResponse<PageResponse<LedgerRowResponse>>>> Ledger([FromQuery] PageRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<LedgerRowResponse>>.Ok(await _inv.GetLedgerAsync(branchId, req, ct));
    }
}
