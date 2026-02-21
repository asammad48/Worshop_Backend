using Api.Security;
using Application.DTOs.Attendance;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<ApiResponse<object>>> CheckIn(AttendanceCheckInRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var branchId = User.GetBranchId();
        await _attendanceService.CheckInAsync(userId, branchId, req, ct);
        return Ok(ApiResponse<object>.Ok("Checked in successfully"));
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<ApiResponse<object>>> CheckOut(AttendanceCheckOutRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _attendanceService.CheckOutAsync(userId, req, ct);
        return Ok(ApiResponse<object>.Ok("Checked out successfully"));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<AttendanceMonthResponse>>> GetMyMonth([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var res = await _attendanceService.GetMyMonthAsync(userId, year, month, ct);
        return Ok(ApiResponse<AttendanceMonthResponse>.Ok(res));
    }
}
