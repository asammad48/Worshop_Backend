using Application.DTOs.Drivers;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/drivers")]
[Authorize]
public sealed class DriversController : ControllerBase
{
    private readonly IDriverService _service;

    public DriversController(IDriverService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> Create([FromBody] DriverCreateRequest request, CancellationToken ct)
        => ApiResponse<DriverResponse>.Ok(await _service.CreateAsync(request, ct));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<DriverResponse>>>> GetPaged([FromQuery] DriverQueryRequest request, CancellationToken ct)
        => ApiResponse<PageResponse<DriverResponse>>.Ok(await _service.GetPagedAsync(request, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> GetById(Guid id, CancellationToken ct)
        => ApiResponse<DriverResponse>.Ok(await _service.GetByIdAsync(id, ct));
}
