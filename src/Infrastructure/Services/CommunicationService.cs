using Application.DTOs.Communications;
using Application.Services.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class CommunicationService : ICommunicationService
{
    private readonly AppDbContext _db;

    public CommunicationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommunicationLogResponse> CreateAsync(Guid actorUserId, Guid branchId, CommunicationLogCreateRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.JobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        var log = new CommunicationLog
        {
            BranchId = branchId,
            JobCardId = request.JobCardId,
            Channel = request.Channel,
            MessageType = request.MessageType,
            SentAt = DateTimeOffset.UtcNow,
            Notes = request.Notes,
            SentByUserId = actorUserId,
            CreatedBy = actorUserId
        };

        _db.CommunicationLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return Map(log);
    }

    public async Task<IReadOnlyList<CommunicationLogResponse>> ListByJobCardAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await _db.CommunicationLogs.AsNoTracking()
            .Where(x => x.JobCardId == jobCardId && x.BranchId == branchId && !x.IsDeleted)
            .OrderByDescending(x => x.SentAt)
            .Select(x => new CommunicationLogResponse(x.Id, x.BranchId, x.JobCardId, x.Channel, x.MessageType, x.SentAt, x.Notes, x.SentByUserId))
            .ToListAsync(ct);
    }

    private static CommunicationLogResponse Map(CommunicationLog x)
        => new(x.Id, x.BranchId, x.JobCardId, x.Channel, x.MessageType, x.SentAt, x.Notes, x.SentByUserId);
}
