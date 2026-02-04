using Application.DTOs.Users;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "HQOnly")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto req, CancellationToken ct)
        => ApiResponse<UserDto>.Ok(await _users.CreateAsync(req, ct));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<UserDto>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
        => ApiResponse<PageResponse<UserDto>>.Ok(await _users.GetPagedAsync(req, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken ct)
        => ApiResponse<UserDto>.Ok(await _users.GetByIdAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserDto req, CancellationToken ct)
        => ApiResponse<UserDto>.Ok(await _users.UpdateAsync(id, req, ct));

    [HttpPost("{id:guid}/set-password")]
    public async Task<ActionResult<ApiResponse<string>>> SetPassword(Guid id, [FromBody] ResetPasswordDto req, CancellationToken ct)
    {
        await _users.SetPasswordAsync(id, req, ct);
        return ApiResponse<string>.Ok("Password updated successfully");
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult<ApiResponse<string>>> Disable(Guid id, CancellationToken ct)
    {
        await _users.DisableAsync(id, ct);
        return ApiResponse<string>.Ok("User disabled successfully");
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult<ApiResponse<string>>> Enable(Guid id, CancellationToken ct)
    {
        await _users.EnableAsync(id, ct);
        return ApiResponse<string>.Ok("User enabled successfully");
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateRole(Guid id, [FromBody] UpdateRoleDto req, CancellationToken ct)
    {
        await _users.UpdateRoleAsync(id, req, ct);
        return ApiResponse<string>.Ok("User role updated successfully");
    }
}
