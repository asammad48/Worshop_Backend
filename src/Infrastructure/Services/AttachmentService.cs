using Application.DTOs.Attachments;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class AttachmentService : IAttachmentService
{
    private readonly AppDbContext _db;
    public AttachmentService(AppDbContext db) { _db = db; }

    public async Task<AttachmentResponse> CreateMetadataAsync(Guid actorUserId, Guid branchId, AttachmentCreateRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.OwnerType)) throw new ValidationException("Validation failed", new[] { "OwnerType required." });
        if (req.OwnerId == Guid.Empty) throw new ValidationException("Validation failed", new[] { "OwnerId required." });
        if (string.IsNullOrWhiteSpace(req.FileName)) throw new ValidationException("Validation failed", new[] { "FileName required." });
        if (string.IsNullOrWhiteSpace(req.StorageKey)) throw new ValidationException("Validation failed", new[] { "StorageKey required." });

        // branch safety for owner types we know
        if (req.OwnerType.Equals("JOBCARD", StringComparison.OrdinalIgnoreCase))
        {
            var ok = await _db.JobCards.AnyAsync(x => x.Id == req.OwnerId && x.BranchId == branchId && !x.IsDeleted, ct);
            if (!ok) throw new NotFoundException("Owner not found");
        }

        var a = new Domain.Entities.Attachment
        {
            OwnerType = req.OwnerType.Trim().ToUpperInvariant(),
            OwnerId = req.OwnerId,
            FileName = req.FileName.Trim(),
            ContentType = req.ContentType.Trim(),
            SizeBytes = req.SizeBytes,
            StorageKey = req.StorageKey.Trim(),
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = actorUserId
        };
        _db.Attachments.Add(a);
        await _db.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task<IReadOnlyList<AttachmentResponse>> ListAsync(Guid branchId, string ownerType, Guid ownerId, CancellationToken ct = default)
    {
        var ot = (ownerType ?? "").Trim().ToUpperInvariant();
        if (ot == "JOBCARD")
        {
            var ok = await _db.JobCards.AnyAsync(x => x.Id == ownerId && x.BranchId == branchId && !x.IsDeleted, ct);
            if (!ok) throw new NotFoundException("Owner not found");
        }

        return await _db.Attachments.AsNoTracking()
            .Where(x => !x.IsDeleted && x.OwnerType == ot && x.OwnerId == ownerId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new AttachmentResponse(x.Id, x.OwnerType, x.OwnerId, x.FileName, x.ContentType, x.SizeBytes, x.StorageKey, x.UploadedAt, x.UploadedByUserId))
            .ToListAsync(ct);
    }

    private static AttachmentResponse Map(Domain.Entities.Attachment x)
        => new(x.Id, x.OwnerType, x.OwnerId, x.FileName, x.ContentType, x.SizeBytes, x.StorageKey, x.UploadedAt, x.UploadedByUserId);
}
