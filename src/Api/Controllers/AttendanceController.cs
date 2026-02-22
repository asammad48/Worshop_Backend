using Api.Security;
using Application.DTOs.Attendance;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _service;

    public AttendanceController(IAttendanceService service)
    {
        _service = service;
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<ApiResponse<AttendanceRecordResponse>>> CheckIn(AttendanceCheckInRequest request, CancellationToken ct)
    {
        var actorId = User.GetUserId();
        Guid? branchId = null;
        var branchClaim = User.FindFirst("branchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var bId)) branchId = bId;

        var result = await _service.CheckInAsync(actorId, branchId, request, ct);
        return Ok(ApiResponse<AttendanceRecordResponse>.Ok(result));
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<ApiResponse<AttendanceRecordResponse>>> CheckOut(AttendanceCheckOutRequest request, CancellationToken ct)
    {
        var actorId = User.GetUserId();
        Guid? branchId = null;
        var branchClaim = User.FindFirst("branchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var bId)) branchId = bId;

        var result = await _service.CheckOutAsync(actorId, branchId, request, ct);
        return Ok(ApiResponse<AttendanceRecordResponse>.Ok(result));
    }

    [HttpGet("today")]
    [Authorize(Policy = "HQManager")]
    public async Task<ActionResult<ApiResponse<PageResponse<AttendanceRecordResponse>>>> GetToday([FromQuery] Guid? branchId, [FromQuery] Guid? employeeUserId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var role = User.GetRole();
        if (role == "BRANCH_MANAGER")
        {
            var myBranchId = User.GetBranchIdOrThrow();
            if (branchId.HasValue && branchId.Value != myBranchId) return Forbid();
            branchId = myBranchId;
        }

        var query = new AttendanceTodayQuery(branchId, employeeUserId, pageNumber, pageSize);
        var result = await _service.GetTodayPagedAsync(query, ct);
        return Ok(ApiResponse<PageResponse<AttendanceRecordResponse>>.Ok(result));
    }

    [HttpGet("employee/{employeeUserId}/month")]
    public async Task<ActionResult<ApiResponse<AttendanceMonthResponse>>> GetMonth(Guid employeeUserId, [FromQuery] int year, [FromQuery] int month, [FromQuery] Guid? branchId, CancellationToken ct)
    {
        var actorId = User.GetUserId();
        var role = User.GetRole();

        if (role == "BRANCH_MANAGER")
        {
            var myBranchId = User.GetBranchIdOrThrow();
            // Manager can view any employee in their branch
            // We'll let the service handle the verification that the employee belongs to the branch if necessary,
            // or just enforce branchId filter.
            if (branchId.HasValue && branchId.Value != myBranchId) return Forbid();
            branchId = myBranchId;
        }
        else if (actorId != employeeUserId && role != "HQ_ADMIN")
        {
            return Forbid();
        }

        var query = new AttendanceEmployeeMonthQuery(employeeUserId, year, month, branchId);
        var result = await _service.GetEmployeeMonthAsync(query, ct);
        return Ok(ApiResponse<AttendanceMonthResponse>.Ok(result));
    }

    [HttpPut("status")]
    [Authorize(Policy = "HQManager")]
    public async Task<ActionResult<ApiResponse<AttendanceRecordResponse>>> UpsertStatus(AttendanceUpsertStatusRequest request, CancellationToken ct)
    {
        var actorId = User.GetUserId();
        Guid? branchId = null;
        var branchClaim = User.FindFirst("branchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var bId)) branchId = bId;

        var result = await _service.UpsertStatusAsync(actorId, branchId, request, ct);
        return Ok(ApiResponse<AttendanceRecordResponse>.Ok(result));
    }
}
