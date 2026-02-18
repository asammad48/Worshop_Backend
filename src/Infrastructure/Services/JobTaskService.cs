using Application.DTOs.JobTasks;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class JobTaskService : IJobTaskService
{
    private readonly AppDbContext _db;
    public JobTaskService(AppDbContext db) { _db = db; }

    public async Task<JobTaskResponse> CreateAsync(Guid actorUserId, Guid branchId, JobTaskCreateRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.JobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        var task = new JobTask
        {
            JobCardId = request.JobCardId,
            StationCode = request.StationCode,
            Title = request.Title,
            Notes = request.Notes,
            Status = JobTaskStatus.Pending,
            CreatedBy = actorUserId
        };

        _db.JobTasks.Add(task);
        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(branchId, task.Id, ct);
    }

    public async Task<JobTaskResponse> StartAsync(Guid actorUserId, Guid branchId, Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.JobTasks
            .Include(x => x.JobCard)
            .FirstOrDefaultAsync(x => x.Id == taskId && !x.IsDeleted, ct);

        if (task is null) throw new NotFoundException("Task not found");
        if (task.JobCard?.BranchId != branchId) throw new ForbiddenException("Wrong branch");

        if (task.Status != JobTaskStatus.Pending)
            throw new ValidationException("Task is not in Pending status", new[] { "StorageKey required." });

        task.Status = JobTaskStatus.InProgress;
        task.StartedAt = DateTimeOffset.UtcNow;
        task.StartedByUserId = actorUserId;
        task.UpdatedBy = actorUserId;

        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(branchId, taskId, ct);
    }

    public async Task<JobTaskResponse> StopAsync(Guid actorUserId, Guid branchId, Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.JobTasks
            .Include(x => x.JobCard)
            .FirstOrDefaultAsync(x => x.Id == taskId && !x.IsDeleted, ct);

        if (task is null) throw new NotFoundException("Task not found");
        if (task.JobCard?.BranchId != branchId) throw new ForbiddenException("Wrong branch");

        if (task.Status != JobTaskStatus.InProgress)
            throw new ValidationException("Task is not In Progress", new[] { "Task is not In Progress." });

        task.Status = JobTaskStatus.Done;
        task.EndedAt = DateTimeOffset.UtcNow;
        task.EndedByUserId = actorUserId;
        task.UpdatedBy = actorUserId;

        if (task.StartedAt.HasValue)
        {
            task.TotalMinutes = (int)(task.EndedAt.Value - task.StartedAt.Value).TotalMinutes;
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(branchId, taskId, ct);
    }

    public async Task<IReadOnlyList<JobTaskResponse>> ListByJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await (from t in _db.JobTasks.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                      join ws in _db.WorkStations on new { Code = t.StationCode, BranchId = branchId } equals new { ws.Code, ws.BranchId } into wsGroup
                      from ws in wsGroup.DefaultIfEmpty()
                      join job in _db.JobCards on t.JobCardId equals job.Id
                      join vehicle in _db.Vehicles on job.VehicleId equals vehicle.Id
                      join creator in _db.Users on t.CreatedBy equals creator.Id into creatorGroup
                      from creator in creatorGroup.DefaultIfEmpty()
                      join starter in _db.Users on t.StartedByUserId equals starter.Id into starterGroup
                      from starter in starterGroup.DefaultIfEmpty()
                      orderby t.CreatedAt
                      select new JobTaskResponse(
                          t.Id, t.JobCardId, t.StationCode, t.Title, t.StartedAt, t.EndedAt, t.StartedByUserId, t.EndedByUserId, t.TotalMinutes, t.Notes, t.Status, t.CreatedAt, t.UpdatedAt,
                          ws.Name, ws.Code, starter.Email, creator.Email, vehicle.Plate
                      )).ToListAsync(ct);
    }

    private async Task<JobTaskResponse> GetByIdInternalAsync(Guid branchId, Guid taskId, CancellationToken ct)
    {
        var task = await (from t in _db.JobTasks.Where(x => x.Id == taskId && !x.IsDeleted)
                          join ws in _db.WorkStations on new { Code = t.StationCode, BranchId = branchId } equals new { ws.Code, ws.BranchId } into wsGroup
                          from ws in wsGroup.DefaultIfEmpty()
                          join job in _db.JobCards on t.JobCardId equals job.Id
                          join vehicle in _db.Vehicles on job.VehicleId equals vehicle.Id
                          join creator in _db.Users on t.CreatedBy equals creator.Id into creatorGroup
                          from creator in creatorGroup.DefaultIfEmpty()
                          join starter in _db.Users on t.StartedByUserId equals starter.Id into starterGroup
                          from starter in starterGroup.DefaultIfEmpty()
                          select new JobTaskResponse(
                              t.Id, t.JobCardId, t.StationCode, t.Title, t.StartedAt, t.EndedAt, t.StartedByUserId, t.EndedByUserId, t.TotalMinutes, t.Notes, t.Status, t.CreatedAt, t.UpdatedAt,
                              ws.Name, ws.Code, starter.Email, creator.Email, vehicle.Plate
                          )).FirstOrDefaultAsync(ct);

        return task ?? throw new NotFoundException("Task not found");
    }

    private static JobTaskResponse Map(JobTask x)
        => new(x.Id, x.JobCardId, x.StationCode, x.Title, x.StartedAt, x.EndedAt, x.StartedByUserId, x.EndedByUserId, x.TotalMinutes, x.Notes, x.Status, x.CreatedAt, x.UpdatedAt);
}
