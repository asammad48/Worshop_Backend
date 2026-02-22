using Application.DTOs.Roadblockers;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class RoadblockerService : IRoadblockerService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    public RoadblockerService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<RoadblockerResponse> CreateAsync(Guid actorUserId, Guid branchId, RoadblockerCreateRequest req, CancellationToken ct = default)
    {
        if (req.JobCardId == Guid.Empty) throw new ValidationException("Validation failed", new[] { "JobCardId required." });

        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.JobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        var rb = new Domain.Entities.Roadblocker
        {
            JobCardId = req.JobCardId,
            Type = req.Type,
            Description = req.Description?.Trim(),
            IsResolved = false,
            CreatedAtLocal = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        _db.Roadblockers.Add(rb);
        await _db.SaveChangesAsync(ct);

        await _notifications.CreateNotificationAsync(
            "JOB_CARD",
            "Roadblocker Added",
            $"New roadblocker on Job Card for vehicle {job.VehicleId}. Description: {rb.Description}",
            "ROADBLOCKER",
            rb.Id,
            null,
            branchId
        );

        return Map(rb);
    }

    public async Task<RoadblockerResponse> ResolveAsync(Guid actorUserId, Guid branchId, Guid roadblockerId, CancellationToken ct = default)
    {
        var rb = await _db.Roadblockers.FirstOrDefaultAsync(x => x.Id == roadblockerId && !x.IsDeleted, ct);
        if (rb is null) throw new NotFoundException("Roadblocker not found");

        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rb.JobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new ForbiddenException("Wrong branch");

        if (!rb.IsResolved)
        {
            rb.IsResolved = true;
            rb.ResolvedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return Map(rb);
    }

    public async Task<IReadOnlyList<RoadblockerResponse>> ListByJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await _db.Roadblockers.AsNoTracking()
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtLocal)
            .Select(x => new RoadblockerResponse(x.Id, x.JobCardId, x.Type, x.Description, x.IsResolved, x.CreatedAtLocal, x.ResolvedAt, x.CreatedByUserId))
            .ToListAsync(ct);
    }

    private static RoadblockerResponse Map(Domain.Entities.Roadblocker x)
        => new(x.Id, x.JobCardId, x.Type, x.Description, x.IsResolved, x.CreatedAtLocal, x.ResolvedAt, x.CreatedByUserId);
}
