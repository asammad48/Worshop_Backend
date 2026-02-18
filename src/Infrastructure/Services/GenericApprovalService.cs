using Application.DTOs.Approvals;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class GenericApprovalService : IGenericApprovalService
{
    private readonly AppDbContext _db;

    public GenericApprovalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApprovalResponse> CreateAsync(Guid actorUserId, Guid branchId, ApprovalCreateRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId && !u.IsDeleted, ct);
        if (user == null) throw new NotFoundException("User not found");

        // Authorization rules
        if (request.ApprovalType == ApprovalRole.Supervisor)
        {
            if (user.Role != UserRole.HQ_ADMIN && user.Role != UserRole.BRANCH_MANAGER)
            {
                throw new ForbiddenException("Only HQ_ADMIN or BRANCH_MANAGER can perform Supervisor approvals");
            }
        }
        else if (request.ApprovalType == ApprovalRole.Cashier)
        {
            if (user.Role != UserRole.HQ_ADMIN && user.Role != UserRole.CASHIER)
            {
                throw new ForbiddenException("Only HQ_ADMIN or CASHIER can perform Cashier approvals");
            }
        }

        var approval = new Approval
        {
            BranchId = branchId,
            TargetType = request.TargetType.ToUpperInvariant(),
            TargetId = request.TargetId,
            ApprovalType = request.ApprovalType,
            ApprovedByUserId = actorUserId,
            ApprovedAt = DateTimeOffset.UtcNow,
            Note = request.Note,
            Status = request.Status,
            CreatedBy = actorUserId
        };

        _db.Approvals.Add(approval);

        // Audit log
        var log = new AuditLog
        {
            BranchId = branchId,
            Action = "Generic Approval Created",
            EntityType = approval.TargetType,
            EntityId = approval.TargetId,
            OldValue = null,
            NewValue = $"Status: {approval.Status}, Type: {approval.ApprovalType}, Note: {approval.Note}",
            PerformedByUserId = actorUserId,
            PerformedAt = DateTimeOffset.UtcNow
        };
        _db.AuditLogs.Add(log);

        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(approval.Id, ct);
    }

    public async Task<IReadOnlyList<ApprovalResponse>> ListAsync(Guid branchId, string targetType, Guid targetId, CancellationToken ct = default)
    {
        return await (from a in _db.Approvals.Where(x => x.BranchId == branchId && x.TargetType == targetType.ToUpperInvariant() && x.TargetId == targetId && !x.IsDeleted)
                      join requester in _db.Users on a.CreatedBy equals requester.Id into requesterGroup
                      from requester in requesterGroup.DefaultIfEmpty()
                      join approver in _db.Users on a.ApprovedByUserId equals approver.Id into approverGroup
                      from approver in approverGroup.DefaultIfEmpty()
                      orderby a.ApprovedAt descending
                      select new ApprovalResponse(
                          a.Id, a.BranchId, a.TargetType, a.TargetId, a.ApprovalType, a.ApprovedByUserId, a.ApprovedAt, a.Note, a.Status,
                          requester.Email, approver.Email
                      )).ToListAsync(ct);
    }

    private async Task<ApprovalResponse> GetByIdInternalAsync(Guid id, CancellationToken ct)
    {
        var approval = await (from a in _db.Approvals.Where(x => x.Id == id && !x.IsDeleted)
                              join requester in _db.Users on a.CreatedBy equals requester.Id into requesterGroup
                              from requester in requesterGroup.DefaultIfEmpty()
                              join approver in _db.Users on a.ApprovedByUserId equals approver.Id into approverGroup
                              from approver in approverGroup.DefaultIfEmpty()
                              select new ApprovalResponse(
                                  a.Id, a.BranchId, a.TargetType, a.TargetId, a.ApprovalType, a.ApprovedByUserId, a.ApprovedAt, a.Note, a.Status,
                                  requester.Email, approver.Email
                              )).FirstOrDefaultAsync(ct);

        return approval ?? throw new NotFoundException("Approval not found");
    }

    private static ApprovalResponse Map(Approval x)
        => new(x.Id, x.BranchId, x.TargetType, x.TargetId, x.ApprovalType, x.ApprovedByUserId, x.ApprovedAt, x.Note, x.Status);
}
