using Application.DTOs.Wages;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/admin/users/{userId}/wage")]
[Authorize(Policy = "HQOnly")]
public sealed class AdminUsersWageController : ControllerBase
{
    private readonly IUserWageService _wageService;

    public AdminUsersWageController(IUserWageService wageService)
    {
        _wageService = wageService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserWageResponse>>> GetWage(Guid userId, CancellationToken ct)
    {
        var res = await _wageService.GetLatestWageAsync(userId, ct);
        return Ok(ApiResponse<UserWageResponse>.Ok(res));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserWageResponse>>> UpsertWage(Guid userId, UserWageUpsertRequest req, CancellationToken ct)
    {
        var res = await _wageService.UpsertWageAsync(userId, req, ct);
        return Ok(ApiResponse<UserWageResponse>.Ok(res));
    }
}
