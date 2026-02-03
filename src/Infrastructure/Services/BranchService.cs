using Application.DTOs.Branches;
using Application.Pagination;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class BranchService : IBranchService
{
    private readonly AppDbContext _db;
    public BranchService(AppDbContext db) { _db = db; }

    public async Task<BranchResponse> CreateAsync(BranchCreateRequest r, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(r.Name))
            throw new ValidationException("Validation failed", new[] { "Name is required." });

        var entity = new Domain.Entities.Branch { Name = r.Name.Trim(), Address = r.Address?.Trim(), IsActive = true };
        _db.Branches.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new BranchResponse(entity.Id, entity.Name, entity.Address, entity.IsActive);
    }

    public async Task<PageResponse<BranchResponse>> GetPagedAsync(PageRequest r, CancellationToken ct = default)
    {
        var q = _db.Branches.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.Name)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new BranchResponse(x.Id, x.Name, x.Address, x.IsActive))
            .ToListAsync(ct);

        return new PageResponse<BranchResponse>(items, total, page, size);
    }

    public async Task<BranchResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (b is null) throw new NotFoundException("Branch not found");
        return new BranchResponse(b.Id, b.Name, b.Address, b.IsActive);
    }
}
