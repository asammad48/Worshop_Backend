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
    private readonly IInvoiceRecomputeQueue _recomputeQueue;
    public TimeLogService(AppDbContext db, IInvoiceRecomputeQueue recomputeQueue)
    {
        _db = db;
        _recomputeQueue = recomputeQueue;
    }

    public async Task<TimeLogResponse> StartAsync(Guid actorUserId, Guid branchId, Guid jobCardId, Guid technicianUserId, Guid jobTaskId, CancellationToken ct = default)
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

        await _recomputeQueue.EnqueueAsync(jobCardId, "Timelog started", ct);

        return await GetByIdInternalAsync(branchId, entity.Id, ct);
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

        await _recomputeQueue.EnqueueAsync(jobCardId, "Timelog stopped", ct);

        return await GetByIdInternalAsync(branchId, log.Id, ct);
    }

    public async Task<IReadOnlyList<TimeLogResponse>> ListAsync(Guid branchId, Guid jobTaskId, CancellationToken ct = default)
    {
        var jobTaskExists = await _db.JobTasks.AnyAsync(x => x.Id == jobTaskId && !x.IsDeleted, ct);

        if (!jobTaskExists) throw new NotFoundException("Job task not found");

        return await (from log in _db.JobCardTimeLogs.Where(x => x.JobTaskId == jobTaskId && !x.IsDeleted)
                      join tech in _db.Users on log.TechnicianUserId equals tech.Id
                      join task in _db.JobTasks on log.JobTaskId equals task.Id
                      join ws in _db.WorkStations on new { Code = task.StationCode, BranchId = branchId } equals new { ws.Code, ws.BranchId } into wsGroup
                      from ws in wsGroup.DefaultIfEmpty()
                      orderby log.StartAt descending
                      select new TimeLogResponse(
                          log.Id, log.JobCardId, log.TechnicianUserId, log.StartAt, log.EndAt, log.TotalMinutes,
                          tech.Email, task.Title, ws.Name
                      )).ToListAsync(ct);
    }

    private async Task<TimeLogResponse> GetByIdInternalAsync(Guid branchId, Guid logId, CancellationToken ct)
    {
        var logEntry = await (from log in _db.JobCardTimeLogs.Where(x => x.Id == logId && !x.IsDeleted)
                              join tech in _db.Users on log.TechnicianUserId equals tech.Id
                              join task in _db.JobTasks on log.JobTaskId equals task.Id
                              join ws in _db.WorkStations on new { Code = task.StationCode, BranchId = branchId } equals new { ws.Code, ws.BranchId } into wsGroup
                              from ws in wsGroup.DefaultIfEmpty()
                              select new TimeLogResponse(
                                  log.Id, log.JobCardId, log.TechnicianUserId, log.StartAt, log.EndAt, log.TotalMinutes,
                                  tech.Email, task.Title, ws.Name
                              )).FirstOrDefaultAsync(ct);

        return logEntry ?? throw new NotFoundException("Time log not found");
    }

    private static TimeLogResponse Map(Domain.Entities.JobCardTimeLog x) =>
        new(x.Id, x.JobCardId, x.TechnicianUserId, x.StartAt, x.EndAt, x.TotalMinutes);
}
