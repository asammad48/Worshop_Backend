using Api.Security;
using Application.DTOs.Finance;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/finance")]
[Authorize(Policy = "BranchUser")]
public sealed class FinanceController : ControllerBase
{
    private readonly IFinanceService _svc;
    public FinanceController(IFinanceService svc) { _svc = svc; }

    [HttpPost("expenses")]
    public async Task<ActionResult<ApiResponse<ExpenseResponse>>> CreateExpense([FromBody] ExpenseCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<ExpenseResponse>.Ok(await _svc.CreateExpenseAsync(userId, branchId, req, ct));
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<ApiResponse<PageResponse<ExpenseResponse>>>> GetExpenses([FromQuery] PageRequest req, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<ExpenseResponse>>.Ok(await _svc.GetExpensesAsync(branchId, req, from, to, ct));
    }

    [HttpPost("wages/pay")]
    public async Task<ActionResult<ApiResponse<WagePayResponse>>> PayWage([FromBody] WagePayRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<WagePayResponse>.Ok(await _svc.PayWageAsync(userId, branchId, req, ct));
    }

    [HttpGet("wages")]
    public async Task<ActionResult<ApiResponse<PageResponse<WagePayResponse>>>> GetWages([FromQuery] PageRequest req, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<WagePayResponse>>.Ok(await _svc.GetWagesAsync(branchId, req, from, to, ct));
    }
}
