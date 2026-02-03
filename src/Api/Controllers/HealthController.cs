using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<string>> Get() => ApiResponse<string>.Ok("OK");
}
