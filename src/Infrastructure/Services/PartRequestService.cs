using Application.DTOs.JobPartRequests;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class PartRequestService : IPartRequestService
{
    private readonly AppDbContext _db;

    public PartRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<JobPartRequestResponse> CreateAsync(Guid actorUserId, Guid branchId, Guid jobCardId, JobPartRequestCreateRequest r, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("JobCard not found");

        var partExists = await _db.Parts.AnyAsync(x => x.Id == r.PartId && !x.IsDeleted, ct);
        if (!partExists) throw new NotFoundException("Part not found");

        var request = new JobPartRequest
        {
            BranchId = branchId,
            JobCardId = jobCardId,
            PartId = r.PartId,
            Qty = r.Qty,
            StationCode = r.StationCode,
            Status = JobPartRequestStatus.Requested,
            RequestedAt = DateTimeOffset.UtcNow
        };

        _db.JobPartRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        return Map(request);
    }

    public async Task<JobPartRequestResponse> MarkOrderedAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        if (request.Status != JobPartRequestStatus.Requested)
            throw new DomainException($"Cannot mark as ordered when status is {request.Status}", 400);

        request.Status = JobPartRequestStatus.Ordered;
        request.OrderedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Map(request);
    }

    public async Task<JobPartRequestResponse> MarkArrivedAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        if (request.Status != JobPartRequestStatus.Ordered)
             throw new DomainException($"Cannot mark as arrived when status is {request.Status}", 400);

        request.Status = JobPartRequestStatus.Arrived;
        request.ArrivedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Map(request);
    }

    public async Task<JobPartRequestResponse> StationSignAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        request.StationSignedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return Map(request);
    }

    public async Task<JobPartRequestResponse> OfficeSignAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        request.OfficeSignedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return Map(request);
    }

    public async Task<IReadOnlyList<JobPartRequestResponse>> ListForJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var requests = await _db.JobPartRequests
            .AsNoTracking()
            .Where(x => x.JobCardId == jobCardId && x.BranchId == branchId && !x.IsDeleted)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(ct);

        return requests.Select(Map).ToList();
    }

    private async Task<JobPartRequest> Load(Guid branchId, Guid id, CancellationToken ct)
    {
        var request = await _db.JobPartRequests.FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId && !x.IsDeleted, ct);
        if (request is null) throw new NotFoundException("Part request not found");
        return request;
    }

    private static JobPartRequestResponse Map(JobPartRequest x) => new(
        x.Id,
        x.BranchId,
        x.JobCardId,
        x.PartId,
        x.Qty,
        x.StationCode,
        x.RequestedAt,
        x.OrderedAt,
        x.ArrivedAt,
        x.StationSignedByUserId,
        x.OfficeSignedByUserId,
        x.Status,
        x.SupplierId,
        x.PurchaseOrderId
    );
}
