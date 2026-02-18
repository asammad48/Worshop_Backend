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
            Notes = notes?.Trim(),
            CreatedBy = actorUserId
        };
        _db.JobCardApprovals.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetByIdInternalAsync(entity.Id, ct);
    }

    public async Task<IReadOnlyList<JobCardApprovalResponse>> ListAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await (from a in _db.JobCardApprovals.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                      join requester in _db.Users on a.CreatedBy equals requester.Id into requesterGroup
                      from requester in requesterGroup.DefaultIfEmpty()
                      join approver in _db.Users on a.ApprovedByUserId equals approver.Id into approverGroup
                      from approver in approverGroup.DefaultIfEmpty()
                      orderby a.ApprovedAt
                      select new JobCardApprovalResponse(
                          a.Id, a.JobCardId, a.Role, a.ApprovedByUserId, a.ApprovedAt, a.Notes,
                          requester.Email, approver.Email
                      )).ToListAsync(ct);
    }

    private async Task<JobCardApprovalResponse> GetByIdInternalAsync(Guid id, CancellationToken ct)
    {
        var approval = await (from a in _db.JobCardApprovals.Where(x => x.Id == id && !x.IsDeleted)
                              join requester in _db.Users on a.CreatedBy equals requester.Id into requesterGroup
                              from requester in requesterGroup.DefaultIfEmpty()
                              join approver in _db.Users on a.ApprovedByUserId equals approver.Id into approverGroup
                              from approver in approverGroup.DefaultIfEmpty()
                              select new JobCardApprovalResponse(
                                  a.Id, a.JobCardId, a.Role, a.ApprovedByUserId, a.ApprovedAt, a.Notes,
                                  requester.Email, approver.Email
                              )).FirstOrDefaultAsync(ct);

        return approval ?? throw new NotFoundException("Approval not found");
    }
}
