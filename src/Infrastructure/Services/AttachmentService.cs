using Application.DTOs.Attachments;
using Application.Services.Interfaces;
using Domain.Enums;
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
            Provider = req.Provider,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = actorUserId
        };
        _db.Attachments.Add(a);
        await _db.SaveChangesAsync(ct);
        return await GetByIdInternalAsync(a.Id, ct);
    }

    public async Task<IReadOnlyList<AttachmentResponse>> ListAsync(Guid branchId, string ownerType, Guid ownerId, CancellationToken ct = default)
    {
        var ot = (ownerType ?? "").Trim().ToUpperInvariant();
        if (ot == "JOBCARD" || ot == "JOB_CARD")
        {
            var ok = await _db.JobCards.AnyAsync(x => x.Id == ownerId && x.BranchId == branchId && !x.IsDeleted, ct);
            if (!ok) throw new NotFoundException("Owner not found");
        }

        var ownerDisplay = await GetOwnerDisplayAsync(ot, ownerId, ct);

        return await (from a in _db.Attachments.Where(x => !x.IsDeleted && x.OwnerType == ot && x.OwnerId == ownerId)
                      join u in _db.Users on a.UploadedByUserId equals u.Id
                      orderby a.UploadedAt descending
                      select new AttachmentResponse(
                          a.Id, a.OwnerType, a.OwnerId, a.FileName, a.ContentType, a.SizeBytes, a.StorageKey, a.Provider, a.UploadedAt, a.UploadedByUserId,
                          u.Email, ownerDisplay
                      )).ToListAsync(ct);
    }

    private async Task<AttachmentResponse> GetByIdInternalAsync(Guid attachmentId, CancellationToken ct)
    {
        var a = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == attachmentId && !x.IsDeleted, ct);
        if (a == null) throw new NotFoundException("Attachment not found");

        var userEmail = await _db.Users.Where(x => x.Id == a.UploadedByUserId).Select(x => x.Email).FirstOrDefaultAsync(ct);
        var ownerDisplay = await GetOwnerDisplayAsync(a.OwnerType, a.OwnerId, ct);

        return new AttachmentResponse(
            a.Id, a.OwnerType, a.OwnerId, a.FileName, a.ContentType, a.SizeBytes, a.StorageKey, a.Provider, a.UploadedAt, a.UploadedByUserId,
            userEmail, ownerDisplay);
    }

    private async Task<string?> GetOwnerDisplayAsync(string ownerType, Guid ownerId, CancellationToken ct)
    {
        var ot = (ownerType ?? "").Trim().ToUpperInvariant();
        if (ot == "JOBCARD" || ot == "JOB_CARD")
        {
            var plate = await _db.JobCards.Where(x => x.Id == ownerId).Select(x => x.Vehicle!.Plate).FirstOrDefaultAsync(ct);
            return $"JOB_CARD — {plate}";
        }
        if (ot == "PURCHASE_ORDER")
        {
            var orderNo = await _db.PurchaseOrders.Where(x => x.Id == ownerId).Select(x => x.OrderNo).FirstOrDefaultAsync(ct);
            return $"PURCHASE_ORDER — {orderNo}";
        }
        return $"{ot} — {ownerId}";
    }

    public async Task<PresignResponse> PresignAsync(Guid actorUserId, Guid branchId, PresignRequest req, CancellationToken ct = default)
    {
        // Placeholder implementation
        var storageKey = $"uploads/{branchId}/{Guid.NewGuid()}-{req.FileName}";
        var dummyUrl = $"https://storage.placeholder.local/{storageKey}?token=dummy";

        return await Task.FromResult(new PresignResponse(dummyUrl, storageKey, StorageProvider.Local));
    }

    private static AttachmentResponse Map(Domain.Entities.Attachment x)
        => new(x.Id, x.OwnerType, x.OwnerId, x.FileName, x.ContentType, x.SizeBytes, x.StorageKey, x.Provider, x.UploadedAt, x.UploadedByUserId);

    private static AttachmentResponse MapStatic(Domain.Entities.Attachment x)
        => new(x.Id, x.OwnerType, x.OwnerId, x.FileName, x.ContentType, x.SizeBytes, x.StorageKey, x.Provider, x.UploadedAt, x.UploadedByUserId);
}
