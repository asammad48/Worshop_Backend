using Application.DTOs.JobCards;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class JobCardService : IJobCardService
{
    private readonly AppDbContext _db;
    public JobCardService(AppDbContext db) { _db = db; }

    public async Task<JobCardResponse> CreateAsync(Guid actorUserId, Guid branchId, JobCardCreateRequest request, CancellationToken ct = default)
    {
        if (branchId == Guid.Empty) throw new ForbiddenException("Branch context required.");
        if (request.VehicleId == Guid.Empty) throw new ValidationException("Validation failed", new[] { "VehicleId is required." });

        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.VehicleId && !x.IsDeleted, ct);
        if (vehicle is null) throw new NotFoundException("Vehicle not found");

        var job = new Domain.Entities.JobCard
        {
            BranchId = branchId,
            VehicleId = vehicle.Id,
            CustomerId = vehicle.CustomerId,
            Mileage = request.Mileage,
            InitialReport = request.InitialReport?.Trim(),
            Status = JobCardStatus.NuevaSolicitud
        };
        _db.JobCards.Add(job);
        await _db.SaveChangesAsync(ct);
        return Map(job);
    }

    public async Task<PageResponse<JobCardResponse>> GetPagedAsync(Guid branchId, PageRequest request, CancellationToken ct = default)
    {
        var q = _db.JobCards.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            // simple search on id string
            q = q.Where(x => x.Id.ToString().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);
        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new JobCardResponse(x.Id, x.BranchId, x.CustomerId, x.VehicleId, x.Status, x.EntryAt, x.ExitAt, x.Mileage, x.InitialReport, x.Diagnosis))
            .ToListAsync(ct);
        return new PageResponse<JobCardResponse>(items, total, page, size);
    }

    public async Task<JobCardResponse> GetByIdAsync(Guid branchId, Guid id, CancellationToken ct = default)
    {
        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.BranchId == branchId, ct);
        if (job is null) throw new NotFoundException("Job card not found");
        return Map(job);
    }

    public async Task<JobCardResponse> CheckInAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);
        if (job.EntryAt is not null) throw new DomainException("Already checked in", 409);
        job.EntryAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(job);
    }

    public async Task<JobCardResponse> CheckOutAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);
        if (job.ExitAt is not null) throw new DomainException("Already checked out", 409);
        if (job.Status != JobCardStatus.Pagado) throw new DomainException("Cannot check out until paid", 409);
        job.ExitAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(job);
    }

    public async Task<JobCardResponse> ChangeStatusAsync(Guid actorUserId, Guid branchId, Guid id, JobCardStatus status, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);
        // simple forward-only rule (allows setting to same or higher)
        if ((short)status < (short)job.Status) throw new DomainException("Invalid status transition", 409);
        job.Status = status;
        await _db.SaveChangesAsync(ct);
        return Map(job);
    }

    public async Task<JobCardResponse> UpdateDiagnosisAsync(Guid actorUserId, Guid branchId, Guid id, string? diagnosis, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);
        job.Diagnosis = diagnosis?.Trim();
        await _db.SaveChangesAsync(ct);
        return Map(job);
    }

    private async Task<Domain.Entities.JobCard> LoadBranchJob(Guid branchId, Guid id, CancellationToken ct)
    {
        var job = await _db.JobCards.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.BranchId == branchId, ct);
        if (job is null) throw new NotFoundException("Job card not found");
        return job;
    }

    private static JobCardResponse Map(Domain.Entities.JobCard x) =>
        new(x.Id, x.BranchId, x.CustomerId, x.VehicleId, x.Status, x.EntryAt, x.ExitAt, x.Mileage, x.InitialReport, x.Diagnosis);
}
