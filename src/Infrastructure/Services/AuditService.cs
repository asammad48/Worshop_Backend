using Application.DTOs.Audit;
using Application.Pagination;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) { _db = db; }

    public async Task<PageResponse<AuditLogResponse>> GetPagedAsync(Guid? branchId, PageRequest req, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking().Where(x => !x.IsDeleted);
        if (branchId is not null) q = q.Where(x => x.BranchId == branchId);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, req.PageNumber);
        var size = Math.Clamp(req.PageSize, 1, 100);

        var items = await q.OrderByDescending(x => x.PerformedAt).Skip((page-1)*size).Take(size)
            .Select(x => new AuditLogResponse(x.Id, x.BranchId, x.Action, x.EntityType, x.EntityId, x.OldValue, x.NewValue, x.PerformedByUserId, x.PerformedAt))
            .ToListAsync(ct);

        return new PageResponse<AuditLogResponse>(items, total, page, size);
    }
}
