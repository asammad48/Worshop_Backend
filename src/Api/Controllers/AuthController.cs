using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) { _auth = auth; }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto req, CancellationToken ct)
        => ApiResponse<LoginResponseDto>.Ok(await _auth.LoginAsync(req, ct));

    [HttpGet("debug-claims")]
    [AllowAnonymous]
    public IActionResult DebugClaims()
    {
        return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
    }
    [HttpGet("auth-test")]
    [AllowAnonymous]
    public IActionResult AuthTest()
    {
        if (User.Identity?.IsAuthenticated ?? false)
            return Ok(new { Authenticated = true, Name = User.Identity.Name, Claims = User.Claims.Select(c => new { c.Type, c.Value }) });
        return Unauthorized();
    }

}
