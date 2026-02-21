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
    private readonly IFileStorage _fileStorage;

    public AttachmentService(AppDbContext db, IFileStorage fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<AttachmentResponse> CreateMetadataAsync(Guid actorUserId, Guid branchId, AttachmentCreateRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.OwnerType)) throw new ValidationException("Validation failed", new[] { "OwnerType required." });
        if (req.OwnerId == Guid.Empty) throw new ValidationException("Validation failed", new[] { "OwnerId required." });
        if (string.IsNullOrWhiteSpace(req.FileName)) throw new ValidationException("Validation failed", new[] { "FileName required." });
        if (string.IsNullOrWhiteSpace(req.StorageKey)) throw new ValidationException("Validation failed", new[] { "StorageKey required." });

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
                          a.Id, a.OwnerType, a.OwnerId, a.FileName, a.ContentType, a.SizeBytes, a.StorageKey, a.Provider, a.Note, a.UploadedAt, a.UploadedByUserId,
                          u.Email, ownerDisplay
                      )).ToListAsync(ct);
    }

    public async Task<AttachmentResponse> UploadAsync(Guid actorUserId, Guid? branchId, string ownerType, Guid ownerId, string? note, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ownerType)) throw new ValidationException("Validation failed", new[] { "OwnerType required." });
        if (ownerId == Guid.Empty) throw new ValidationException("Validation failed", new[] { "OwnerId required." });

        var ot = ownerType.Trim().ToUpperInvariant();
        if (branchId.HasValue && (ot == "JOBCARD" || ot == "JOB_CARD"))
        {
             var ok = await _db.JobCards.AnyAsync(x => x.Id == ownerId && x.BranchId == branchId.Value && !x.IsDeleted, ct);
             if (!ok) throw new NotFoundException("Owner not found or access denied");
        }

        var safeFileName = Path.GetFileName(fileName);
        var pathPattern = $"{ot.ToLowerInvariant()}/{ownerId}/{Guid.NewGuid()}_{safeFileName}";
        var storageKey = await _fileStorage.SaveAsync(pathPattern, content, ct);

        var a = new Domain.Entities.Attachment
        {
            OwnerType = ot,
            OwnerId = ownerId,
            FileName = safeFileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            Provider = StorageProvider.Local,
            Note = note,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = actorUserId
        };

        _db.Attachments.Add(a);
        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(a.Id, ct);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(Guid id, CancellationToken ct = default)
    {
        var a = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (a == null) throw new NotFoundException("Attachment not found");

        var stream = await _fileStorage.OpenReadAsync(a.StorageKey, ct);
        if (stream == null) throw new NotFoundException("File content not found on storage");

        return (stream, a.FileName, a.ContentType);
    }

    private async Task<AttachmentResponse> GetByIdInternalAsync(Guid attachmentId, CancellationToken ct)
    {
        var a = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == attachmentId && !x.IsDeleted, ct);
        if (a == null) throw new NotFoundException("Attachment not found");

        var userEmail = await _db.Users.Where(x => x.Id == a.UploadedByUserId).Select(x => x.Email).FirstOrDefaultAsync(ct);
        var ownerDisplay = await GetOwnerDisplayAsync(a.OwnerType, a.OwnerId, ct);

        return new AttachmentResponse(
            a.Id, a.OwnerType, a.OwnerId, a.FileName, a.ContentType, a.SizeBytes, a.StorageKey, a.Provider, a.Note, a.UploadedAt, a.UploadedByUserId,
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
}
