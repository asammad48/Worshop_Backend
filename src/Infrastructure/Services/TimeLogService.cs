using Application.DTOs.TimeLogs;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class TimeLogService : ITimeLogService
{
    private readonly AppDbContext _db;
    public TimeLogService(AppDbContext db) { _db = db; }

    public async Task<TimeLogResponse> StartAsync(Guid actorUserId, Guid branchId, Guid jobCardId, Guid technicianUserId,Guid jobTaskId, CancellationToken ct = default)
    {
        var jobTaskExists = await _db.JobTasks.AnyAsync(x => x.Id == jobTaskId && !x.IsDeleted, ct);
        if (!jobTaskExists) throw new NotFoundException("Job task not found");

        var tech = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == technicianUserId && x.BranchId == branchId && !x.IsDeleted && x.IsActive, ct);
        if (tech is null || tech.Role != UserRole.TECHNICIAN) throw new DomainException("Technician invalid", 409);

        var openExists = await _db.JobCardTimeLogs.AnyAsync(x => x.JobTaskId == jobCardId && x.TechnicianUserId == technicianUserId && x.EndAt == null && !x.IsDeleted, ct);
        if (openExists) throw new DomainException("Time log already running", 409);

        var entity = new Domain.Entities.JobCardTimeLog
        {
            JobTaskId = jobTaskId,
            JobCardId = jobCardId,
            TechnicianUserId = technicianUserId,
            StartAt = DateTimeOffset.UtcNow,
            EndAt = null,
            TotalMinutes = 0
        };
        _db.JobCardTimeLogs.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<TimeLogResponse> StopAsync(Guid actorUserId, Guid branchId, Guid jobCardId, Guid timeLogId, CancellationToken ct = default)
    {
        var log = await _db.JobCardTimeLogs.FirstOrDefaultAsync(x => x.Id == timeLogId && x.JobCardId == jobCardId && !x.IsDeleted, ct);
        if (log is null) throw new NotFoundException("Time log not found");

        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        if (log.EndAt is not null) throw new DomainException("Time log already stopped", 409);

        log.EndAt = DateTimeOffset.UtcNow;
        log.TotalMinutes = (int)Math.Max(0, (log.EndAt.Value - log.StartAt).TotalMinutes);
        await _db.SaveChangesAsync(ct);
        return Map(log);
    }

    public async Task<IReadOnlyList<TimeLogResponse>> ListAsync(Guid branchId, Guid jobTaskId, CancellationToken ct = default)
    {
        var jobTaskExists = await _db.JobTasks.AnyAsync(x => x.Id == jobTaskId && !x.IsDeleted, ct);

        if (!jobTaskExists) throw new NotFoundException("Job task not found");

        return await _db.JobCardTimeLogs.AsNoTracking()
            .Where(x => x.JobTaskId == jobTaskId && !x.IsDeleted)
            .OrderByDescending(x => x.StartAt)
            .Select(x => new TimeLogResponse(x.Id, x.JobCardId, x.TechnicianUserId, x.StartAt, x.EndAt, x.TotalMinutes))
            .ToListAsync(ct);
    }

    private static TimeLogResponse Map(Domain.Entities.JobCardTimeLog x) =>
        new(x.Id, x.JobCardId, x.TechnicianUserId, x.StartAt, x.EndAt, x.TotalMinutes);
}
