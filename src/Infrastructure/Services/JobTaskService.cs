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

        return Map(task);
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

        return Map(task);
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

        return Map(task);
    }

    public async Task<IReadOnlyList<JobTaskResponse>> ListByJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await _db.JobTasks.AsNoTracking()
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new JobTaskResponse(x.Id, x.JobCardId, x.StationCode, x.Title, x.StartedAt, x.EndedAt, x.StartedByUserId, x.EndedByUserId, x.TotalMinutes, x.Notes, x.Status, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);
    }

    private static JobTaskResponse Map(JobTask x)
        => new(x.Id, x.JobCardId, x.StationCode, x.Title, x.StartedAt, x.EndedAt, x.StartedByUserId, x.EndedByUserId, x.TotalMinutes, x.Notes, x.Status, x.CreatedAt, x.UpdatedAt);
}
