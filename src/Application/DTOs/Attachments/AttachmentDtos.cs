using Domain.Enums;

namespace Application.DTOs.Attachments;

public sealed record AttachmentCreateRequest(
    string OwnerType,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageKey,
    StorageProvider Provider = StorageProvider.Local
);

public sealed record AttachmentResponse(
    Guid Id,
    string OwnerType,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageKey,
    StorageProvider Provider,
    string? Note,
    DateTimeOffset UploadedAt,
    Guid UploadedByUserId,
    string? UploadedByEmail = null,
    string? OwnerDisplay = null
);

