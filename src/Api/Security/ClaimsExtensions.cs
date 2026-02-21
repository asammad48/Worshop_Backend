using System.Security.Claims;
using Shared.Errors;

namespace Api.Security;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var id)) throw new ForbiddenException("Invalid user context");
        return id;
    }

    public static Guid GetBranchIdOrThrow(this ClaimsPrincipal user)
    {
        var b = user.FindFirstValue("branchId");
        if (b is null || !Guid.TryParse(b, out var id)) throw new ForbiddenException("Branch context missing");
        return id;
    }

    public static Guid? GetBranchId(this ClaimsPrincipal user)
    {
        var b = user.FindFirstValue("branchId");
        if (b is null || !Guid.TryParse(b, out var id)) return null;
        return id;
    }

    public static string GetRole(this ClaimsPrincipal user) => user.FindFirstValue("role") ?? "";
}
