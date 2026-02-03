using Application.DTOs.Vehicles;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize]
public sealed class VehiclesController : ControllerBase
{
    private readonly IVehicleService _svc;
    public VehiclesController(IVehicleService svc) { _svc = svc; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> Create([FromBody] VehicleCreateRequest req, CancellationToken ct)
        => ApiResponse<VehicleResponse>.Ok(await _svc.CreateAsync(req, ct));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<VehicleResponse>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
        => ApiResponse<PageResponse<VehicleResponse>>.Ok(await _svc.GetPagedAsync(req, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> GetById(Guid id, CancellationToken ct)
        => ApiResponse<VehicleResponse>.Ok(await _svc.GetByIdAsync(id, ct));
}
