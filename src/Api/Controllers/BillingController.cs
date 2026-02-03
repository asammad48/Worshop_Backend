using Api.Security;
using Application.DTOs.Billing;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Authorize(Policy = "BranchUser")]
public sealed class BillingController : ControllerBase
{
    private readonly IBillingService _billing;
    public BillingController(IBillingService billing) { _billing = billing; }

    [HttpPost("invoices/{invoiceId:guid}/payments")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> AddPayment(Guid invoiceId, [FromBody] PaymentCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<PaymentResponse>.Ok(await _billing.AddPaymentAsync(userId, branchId, invoiceId, req, ct));
    }

    [HttpGet("invoices/{invoiceId:guid}/payments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentResponse>>>> ListPayments(Guid invoiceId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<PaymentResponse>>.Ok(await _billing.ListPaymentsAsync(branchId, invoiceId, ct));
    }
}
