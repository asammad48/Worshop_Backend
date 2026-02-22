using Api.Security;
using Application.DTOs.Dashboard;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers.v1;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardOverviewResponse>>> GetOverview([FromQuery] DashboardOverviewQuery query)
    {
        var userId = User.GetUserId();
        var roleStr = User.GetRole();
        if (!Enum.TryParse<UserRole>(roleStr, true, out var role)) role = UserRole.TECHNICIAN;

        return ApiResponse<DashboardOverviewResponse>.Ok(await _dashboard.GetOverviewAsync(query, role, userId));
    }

    [HttpGet("jobcards")]
    public async Task<ActionResult<ApiResponse<PageResponse<JobCardAlertRow>>>> GetJobCards([FromQuery] Guid? branchId, [FromQuery] string? status, [FromQuery] int? minDaysInShop, [FromQuery] bool? hasRoadblocker, [FromQuery] ApprovalRole? requiresApprovalRole, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        return ApiResponse<PageResponse<JobCardAlertRow>>.Ok(await _dashboard.GetJobCardAlertsAsync(branchId, status, minDaysInShop, hasRoadblocker, requiresApprovalRole, pageNumber, pageSize));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<ApiResponse<InventoryDashboardResponse>>> GetInventory([FromQuery] Guid? branchId, [FromQuery] bool belowReorderOnly = true, [FromQuery] bool? pendingPoOnly = null, [FromQuery] bool? pendingTransfersOnly = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        return ApiResponse<InventoryDashboardResponse>.Ok(await _dashboard.GetInventoryDashboardAsync(branchId, belowReorderOnly, pendingPoOnly, pendingTransfersOnly, pageNumber, pageSize));
    }

    [HttpGet("employees/kpi")]
    public async Task<ActionResult<ApiResponse<PageResponse<EmployeeKpiRow>>>> GetEmployeeKpis([FromQuery] Guid? branchId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? employeeUserId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        return ApiResponse<PageResponse<EmployeeKpiRow>>.Ok(await _dashboard.GetEmployeeKpisAsync(branchId, from, to, employeeUserId, pageNumber, pageSize));
    }
}
