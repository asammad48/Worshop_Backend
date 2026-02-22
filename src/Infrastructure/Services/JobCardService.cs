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
    private readonly INotificationService _notifications;
    public JobCardService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

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
        return await GetByIdAsync(branchId, job.Id, ct);
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
            .Select(x => new JobCardResponse(
                x.Id,
                x.BranchId,
                x.CustomerId,
                x.VehicleId,
                x.Status,
                x.EntryAt,
                x.ExitAt,
                x.Mileage,
                x.InitialReport,
                x.Diagnosis,
                x.Customer != null ? x.Customer.FullName : null,
                x.Vehicle != null ? x.Vehicle.Plate : null,
                x.Branch != null ? x.Branch.Name : null,
                _db.JobCardWorkStationHistories
                    .Where(h => h.JobCardId == x.Id && !h.IsDeleted)
                    .OrderByDescending(h => h.MovedAt)
                    .Select(h => h.WorkStation != null ? h.WorkStation.Name : null)
                    .FirstOrDefault(),
                _db.JobCardWorkStationHistories
                    .Where(h => h.JobCardId == x.Id && !h.IsDeleted)
                    .OrderByDescending(h => h.MovedAt)
                    .Select(h => h.WorkStation != null ? h.WorkStation.Code : null)
                    .FirstOrDefault()
            ))
            .ToListAsync(ct);
        return new PageResponse<JobCardResponse>(items, total, page, size);
    }

    public async Task<JobCardResponse> GetByIdAsync(Guid branchId, Guid id, CancellationToken ct = default)
    {
        var x = await _db.JobCards.AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.Vehicle)
            .Include(j => j.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.BranchId == branchId, ct);
        if (x is null) throw new NotFoundException("Job card not found");

        var station = await _db.JobCardWorkStationHistories
            .Where(h => h.JobCardId == x.Id && !h.IsDeleted)
            .OrderByDescending(h => h.MovedAt)
            .Select(h => new {
                Name = h.WorkStation != null ? h.WorkStation.Name : null,
                Code = h.WorkStation != null ? h.WorkStation.Code : null
            })
            .FirstOrDefaultAsync(ct);

        return new JobCardResponse(
            x.Id, x.BranchId, x.CustomerId, x.VehicleId, x.Status, x.EntryAt, x.ExitAt, x.Mileage, x.InitialReport, x.Diagnosis,
            x.Customer?.FullName, x.Vehicle?.Plate, x.Branch?.Name,
            station?.Name, station?.Code);
    }

    public async Task<JobCardResponse> CheckInAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);
        if (job.EntryAt is not null) throw new DomainException("Already checked in", 409);
        job.EntryAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(branchId, id, ct);
    }

    public async Task<JobCardResponse> CheckOutAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);
        if (job.ExitAt is not null) throw new DomainException("Already checked out", 409);
        if (job.Status != JobCardStatus.Pagado) throw new DomainException("Cannot check out until paid", 409);
        job.ExitAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(branchId, id, ct);
    }

    public async Task<JobCardResponse> ChangeStatusAsync(Guid actorUserId, Guid branchId, Guid id, JobCardStatus status, string? note = null, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);

        // Transition Rules
        if (status == job.Status) return Map(job);

        // 1. Forward-only mostly, but allow some flexibility if needed?
        // User example: "cannot go Pagado without billing paid"
        if (status == JobCardStatus.Pagado)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.JobCardId == job.Id && !x.IsDeleted, ct);
            if (invoice == null || invoice.PaymentStatus != PaymentStatus.Paid)
            {
                throw new DomainException("Cannot set status to Pagado until invoice is fully paid", 400);
            }
        }

        var oldStatus = job.Status;
        job.Status = status;

        _db.AuditLogs.Add(new Domain.Entities.AuditLog
        {
            BranchId = branchId,
            Action = "JOB_CARD_STATUS_CHANGE",
            EntityType = "JobCard",
            EntityId = job.Id,
            OldValue = oldStatus.ToString(),
            NewValue = string.IsNullOrWhiteSpace(note) ? status.ToString() : $"{status} | Note: {note}",
            PerformedByUserId = actorUserId,
            PerformedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        if (status == JobCardStatus.EsperandoAprobacion)
        {
            await _notifications.CreateNotificationAsync(
                "APPROVAL",
                "Approval Required",
                $"Job Card {id} is waiting for approval.",
                "JOB_CARD",
                id,
                null,
                branchId
            );
        }

        return await GetByIdAsync(branchId, id, ct);
    }

    public async Task<JobCardResponse> UpdateDiagnosisAsync(Guid actorUserId, Guid branchId, Guid id, string? diagnosis, CancellationToken ct = default)
    {
        var job = await LoadBranchJob(branchId, id, ct);
        job.Diagnosis = diagnosis?.Trim();
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(branchId, id, ct);
    }

    private async Task<Domain.Entities.JobCard> LoadBranchJob(Guid branchId, Guid id, CancellationToken ct)
    {
        var job = await _db.JobCards
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.BranchId == branchId, ct);
        if (job is null) throw new NotFoundException("Job card not found");
        return job;
    }

    private static JobCardResponse Map(Domain.Entities.JobCard x) =>
        new(
            x.Id,
            x.BranchId,
            x.CustomerId,
            x.VehicleId,
            x.Status,
            x.EntryAt,
            x.ExitAt,
            x.Mileage,
            x.InitialReport,
            x.Diagnosis,
            x.Customer?.FullName,
            x.Vehicle?.Plate,
            x.Branch?.Name,
            null, // Map cannot easily fetch history without DB context
            null
        );
}
