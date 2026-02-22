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
    private readonly INotificationService _notifications;

    public PartRequestService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
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
            RequestedAt = DateTimeOffset.UtcNow,
            CreatedBy = actorUserId
        };

        _db.JobPartRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        await _notifications.CreateNotificationAsync(
            "INVENTORY",
            "Part Requested",
            $"Part {r.PartId} requested for Job Card {jobCardId} at station {r.StationCode}.",
            "PART_REQUEST",
            request.Id,
            null,
            branchId
        );

        return await GetByIdInternalAsync(branchId, request.Id, ct);
    }

    public async Task<JobPartRequestResponse> MarkOrderedAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        if (request.Status != JobPartRequestStatus.Requested)
            throw new DomainException($"Cannot mark as ordered when status is {request.Status}", 400);

        request.Status = JobPartRequestStatus.Ordered;
        request.OrderedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdInternalAsync(branchId, id, ct);
    }

    public async Task<JobPartRequestResponse> MarkArrivedAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        if (request.Status != JobPartRequestStatus.Ordered)
             throw new DomainException($"Cannot mark as arrived when status is {request.Status}", 400);

        request.Status = JobPartRequestStatus.Arrived;
        request.ArrivedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdInternalAsync(branchId, id, ct);
    }

    public async Task<JobPartRequestResponse> StationSignAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        request.StationSignedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return await GetByIdInternalAsync(branchId, id, ct);
    }

    public async Task<JobPartRequestResponse> OfficeSignAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var request = await Load(branchId, id, ct);
        request.OfficeSignedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return await GetByIdInternalAsync(branchId, id, ct);
    }

    public async Task<IReadOnlyList<JobPartRequestResponse>> ListForJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        return await (from r in _db.JobPartRequests.Where(x => x.JobCardId == jobCardId && x.BranchId == branchId && !x.IsDeleted)
                      join part in _db.Parts on r.PartId equals part.Id
                      join supplier in _db.Suppliers on r.SupplierId equals supplier.Id into supplierGroup
                      from supplier in supplierGroup.DefaultIfEmpty()
                      join user in _db.Users on r.CreatedBy equals user.Id into userGroup
                      from user in userGroup.DefaultIfEmpty()
                      join ws in _db.WorkStations on new { Code = r.StationCode, BranchId = branchId } equals new { ws.Code, ws.BranchId } into wsGroup
                      from ws in wsGroup.DefaultIfEmpty()
                      orderby r.RequestedAt descending
                      select new JobPartRequestResponse(
                          r.Id, r.BranchId, r.JobCardId, r.PartId, r.Qty, r.StationCode, r.RequestedAt, r.OrderedAt, r.ArrivedAt,
                          r.StationSignedByUserId, r.OfficeSignedByUserId, r.Status, r.SupplierId, r.PurchaseOrderId,
                          part.Sku, part.Name, supplier.Name, user.Email, ws.Name
                      )).ToListAsync(ct);
    }

    private async Task<JobPartRequestResponse> GetByIdInternalAsync(Guid branchId, Guid id, CancellationToken ct)
    {
        var response = await (from r in _db.JobPartRequests.Where(x => x.Id == id && x.BranchId == branchId && !x.IsDeleted)
                              join part in _db.Parts on r.PartId equals part.Id
                              join supplier in _db.Suppliers on r.SupplierId equals supplier.Id into supplierGroup
                              from supplier in supplierGroup.DefaultIfEmpty()
                              join user in _db.Users on r.CreatedBy equals user.Id into userGroup
                              from user in userGroup.DefaultIfEmpty()
                              join ws in _db.WorkStations on new { Code = r.StationCode, BranchId = branchId } equals new { ws.Code, ws.BranchId } into wsGroup
                              from ws in wsGroup.DefaultIfEmpty()
                              select new JobPartRequestResponse(
                                  r.Id, r.BranchId, r.JobCardId, r.PartId, r.Qty, r.StationCode, r.RequestedAt, r.OrderedAt, r.ArrivedAt,
                                  r.StationSignedByUserId, r.OfficeSignedByUserId, r.Status, r.SupplierId, r.PurchaseOrderId,
                                  part.Sku, part.Name, supplier.Name, user.Email, ws.Name
                              )).FirstOrDefaultAsync(ct);

        return response ?? throw new NotFoundException("Part request not found");
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
