using Application.DTOs.WorkStations;
using Application.Pagination;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class WorkStationService : IWorkStationService
{
    private readonly AppDbContext _db;
    public WorkStationService(AppDbContext db) { _db = db; }

    public async Task<WorkStationResponse> CreateAsync(Guid branchId, WorkStationCreateRequest request, CancellationToken ct = default)
    {
        if (branchId == Guid.Empty) throw new ForbiddenException("Branch context required.");
        var code = (request.Code ?? "").Trim().ToUpperInvariant();
        var name = (request.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Validation failed", new[] { "Code and Name are required." });

        var exists = await _db.WorkStations.AnyAsync(x => x.BranchId == branchId && x.Code == code && !x.IsDeleted, ct);
        if (exists) throw new DomainException("WorkStation code already exists", 409);

        var entity = new Domain.Entities.WorkStation { BranchId = branchId, Code = code, Name = name, IsActive = true };
        _db.WorkStations.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new WorkStationResponse(entity.Id, entity.BranchId, entity.Code, entity.Name, entity.IsActive);
    }

    public async Task<PageResponse<WorkStationResponse>> GetPagedAsync(Guid branchId, PageRequest request, CancellationToken ct = default)
    {
        var q = _db.WorkStations.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToUpperInvariant();
            q = q.Where(x => x.Code.Contains(s) || x.Name.ToUpper().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.Code)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new WorkStationResponse(x.Id, x.BranchId, x.Code, x.Name, x.IsActive))
            .ToListAsync(ct);
        return new PageResponse<WorkStationResponse>(items, total, page, size);
    }

    public async Task<IReadOnlyList<JobCardStationHistoryResponse>> GetJobCardHistoryAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await _db.JobCardWorkStationHistories.AsNoTracking()
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderBy(x => x.MovedAt)
            .Select(x => new JobCardStationHistoryResponse(x.Id, x.JobCardId, x.WorkStationId, x.MovedAt, x.MovedByUserId, x.Notes))
            .ToListAsync(ct);
    }

    public async Task<JobCardStationHistoryResponse> MoveJobCardAsync(Guid actorUserId, Guid branchId, Guid jobCardId, MoveJobCardRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        var ws = await _db.WorkStations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.WorkStationId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (ws is null) throw new NotFoundException("WorkStation not found");

        var hist = new Domain.Entities.JobCardWorkStationHistory
        {
            JobCardId = jobCardId,
            WorkStationId = ws.Id,
            MovedAt = DateTimeOffset.UtcNow,
            MovedByUserId = actorUserId,
            Notes = request.Notes?.Trim()
        };
        _db.JobCardWorkStationHistories.Add(hist);
        await _db.SaveChangesAsync(ct);
        return new JobCardStationHistoryResponse(hist.Id, hist.JobCardId, hist.WorkStationId, hist.MovedAt, hist.MovedByUserId, hist.Notes);
    }
}
