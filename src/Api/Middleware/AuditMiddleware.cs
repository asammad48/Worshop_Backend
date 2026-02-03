using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Shared.Errors;

namespace Api.Middleware;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;
    public AuditMiddleware(RequestDelegate next) { _next = next; }

    public async Task Invoke(HttpContext ctx, AppDbContext db)
    {
        // Only log mutating methods
        var method = ctx.Request.Method.ToUpperInvariant();
        var shouldLog = method is "POST" or "PUT" or "PATCH" or "DELETE";
        string? body = null;

        if (shouldLog)
        {
            // very lightweight snapshot: path + query (no body to avoid large payloads)
            body = JsonSerializer.Serialize(new
            {
                path = ctx.Request.Path.ToString(),
                query = ctx.Request.QueryString.ToString()
            });
        }

        await _next(ctx);

        if (!shouldLog) return;

        // Only log successful responses
        if (ctx.Response.StatusCode < 200 || ctx.Response.StatusCode >= 400) return;

        Guid? branchId = null;
        Guid performedBy = Guid.Empty;

        var b = ctx.User?.FindFirst("branchId")?.Value;
        if (Guid.TryParse(b, out var bid)) branchId = bid;

        var sub = ctx.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out performedBy)) performedBy = Guid.Empty;

        var action = $"{method} {ctx.Request.Path}";
        var log = new Domain.Entities.AuditLog
        {
            BranchId = branchId,
            Action = action,
            EntityType = "HTTP",
            EntityId = Guid.NewGuid(),
            OldValue = null,
            NewValue = body,
            PerformedByUserId = performedBy,
            PerformedAt = DateTimeOffset.UtcNow
        };
        db.AuditLogs.Add(log);
        try { await db.SaveChangesAsync(); } catch { /* don't block requests */ }
    }
}
