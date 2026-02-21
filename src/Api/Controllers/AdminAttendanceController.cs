using Api.Security;
using Application.DTOs.Attendance;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;
using Shared.Errors;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/admin/attendance")]
[Authorize(Policy = "HQManager")]
public sealed class AdminAttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IUserService _userService;

    public AdminAttendanceController(IAttendanceService attendanceService, IUserService userService)
    {
        _attendanceService = attendanceService;
        _userService = userService;
    }

    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AttendanceDaySummaryResponse>>>> GetToday([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                   ?? User.FindFirst("role")?.Value;

        if (role == "BRANCH_MANAGER")
        {
            var userBranchId = User.GetBranchIdOrThrow();
            if (branchId != null && branchId != userBranchId) throw new ForbiddenException("Cannot view other branch attendance");
            branchId = userBranchId;
        }

        var res = await _attendanceService.GetTodayAttendanceAsync(branchId, ct);
        return Ok(ApiResponse<IReadOnlyList<AttendanceDaySummaryResponse>>.Ok(res));
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<ApiResponse<AttendanceMonthResponse>>> GetUserMonth(Guid userId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                   ?? User.FindFirst("role")?.Value;

        if (role == "BRANCH_MANAGER")
        {
            var userBranchId = User.GetBranchIdOrThrow();
            var targetUser = await _userService.GetByIdAsync(userId, ct);
            if (targetUser.BranchId != userBranchId) throw new ForbiddenException("Cannot view other branch attendance from different branch");
        }

        var res = await _attendanceService.GetUserMonthAsync(userId, year, month, ct);
        return Ok(ApiResponse<AttendanceMonthResponse>.Ok(res));
    }
}
