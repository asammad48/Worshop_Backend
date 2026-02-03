using Api.Security;
using Application.DTOs.Audit;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Policy = "HQOnly")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _svc;
    public AuditController(IAuditService svc) { _svc = svc; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<AuditLogResponse>>>> Get([FromQuery] PageRequest req, [FromQuery] Guid? branchId, CancellationToken ct)
        => ApiResponse<PageResponse<AuditLogResponse>>.Ok(await _svc.GetPagedAsync(branchId, req, ct));
}
