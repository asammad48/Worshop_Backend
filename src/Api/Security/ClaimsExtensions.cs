using System.Security.Claims;
using Shared.Errors;

namespace Api.Security;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub");
        if (sub is null || !Guid.TryParse(sub, out var id)) throw new ForbiddenException("Invalid user context");
        return id;
    }

    public static Guid GetBranchIdOrThrow(this ClaimsPrincipal user)
    {
        var b = user.FindFirstValue("branchId");
        if (b is null || !Guid.TryParse(b, out var id)) throw new ForbiddenException("Branch context missing");
        return id;
    }

    public static string GetRole(this ClaimsPrincipal user) => user.FindFirstValue("role") ?? "";
}
