using Application.DTOs.Billing;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize(Policy = "HQManager")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceRecomputeQueue _recomputeQueue;

    public InvoicesController(IInvoiceRecomputeQueue recomputeQueue)
    {
        _recomputeQueue = recomputeQueue;
    }

    [HttpPost("recompute")]
    public async Task<ActionResult<ApiResponse<string>>> Recompute(InvoiceRecomputeRequest req, CancellationToken ct)
    {
        await _recomputeQueue.EnqueueAsync(req.JobCardId, req.Reason ?? "Manual trigger", ct);
        return Ok(ApiResponse<string>.Ok("Recompute job enqueued"));
    }
}
