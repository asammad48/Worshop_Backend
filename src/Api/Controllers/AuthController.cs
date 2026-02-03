using Application.DTOs.Auth;
using Application.Services.Interfaces;
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
}
