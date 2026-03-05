using Application.DTOs.Printing;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("public/receipt/jobcards")]
public sealed class PublicReceiptController : ControllerBase
{
    private readonly IReceiptService _receipts;
    private readonly IPrintService _prints;

    public PublicReceiptController(IReceiptService receipts, IPrintService prints)
    {
        _receipts = receipts;
        _prints = prints;
    }

    [HttpGet("{jobCardId:guid}")]
    public async Task<ActionResult<ApiResponse<PublicJobCardReceiptResponse>>> Get(Guid jobCardId, [FromQuery] string? t, CancellationToken ct)
    {
        return ApiResponse<PublicJobCardReceiptResponse>.Ok(await _receipts.GetPublicReceiptAsync(jobCardId, t, ct));
    }

    [HttpGet("{jobCardId:guid}/print")]
    public async Task<IActionResult> Print(Guid jobCardId, [FromQuery] string? t, CancellationToken ct)
    {
        var pdf = await _prints.RenderPublicReceiptPdfAsync(jobCardId, t, ct);
        return File(pdf, "application/pdf");
    }
}
