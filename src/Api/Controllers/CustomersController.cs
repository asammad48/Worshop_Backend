using Application.DTOs.Customers;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _svc;
    public CustomersController(ICustomerService svc) { _svc = svc; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create([FromBody] CustomerCreateRequest req, CancellationToken ct)
        => ApiResponse<CustomerResponse>.Ok(await _svc.CreateAsync(req, ct));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<CustomerResponse>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
        => ApiResponse<PageResponse<CustomerResponse>>.Ok(await _svc.GetPagedAsync(req, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetById(Guid id, CancellationToken ct)
        => ApiResponse<CustomerResponse>.Ok(await _svc.GetByIdAsync(id, ct));
}
