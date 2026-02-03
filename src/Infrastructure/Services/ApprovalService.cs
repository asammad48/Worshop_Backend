using Application.DTOs.Approvals;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class ApprovalService : IApprovalService
{
    private readonly AppDbContext _db;
    public ApprovalService(AppDbContext db) { _db = db; }

    public async Task<JobCardApprovalResponse> ApproveAsync(Guid actorUserId, Guid branchId, Guid jobCardId, ApprovalRole role, string? notes, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        var exists = await _db.JobCardApprovals.AnyAsync(x => x.JobCardId == jobCardId && x.Role == role && !x.IsDeleted, ct);
        if (exists) throw new DomainException("Already approved for this role", 409);

        var entity = new Domain.Entities.JobCardApproval
        {
            JobCardId = jobCardId,
            Role = role,
            ApprovedByUserId = actorUserId,
            ApprovedAt = DateTimeOffset.UtcNow,
            Notes = notes?.Trim()
        };
        _db.JobCardApprovals.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new JobCardApprovalResponse(entity.Id, entity.JobCardId, entity.Role, entity.ApprovedByUserId, entity.ApprovedAt, entity.Notes);
    }

    public async Task<IReadOnlyList<JobCardApprovalResponse>> ListAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await _db.JobCardApprovals.AsNoTracking()
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderBy(x => x.ApprovedAt)
            .Select(x => new JobCardApprovalResponse(x.Id, x.JobCardId, x.Role, x.ApprovedByUserId, x.ApprovedAt, x.Notes))
            .ToListAsync(ct);
    }
}
