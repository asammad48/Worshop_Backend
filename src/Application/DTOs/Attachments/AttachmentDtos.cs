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
    DateTimeOffset UploadedAt,
    Guid UploadedByUserId
);

public sealed record PresignRequest(string FileName, string ContentType);
public sealed record PresignResponse(string UploadUrl, string StorageKey, StorageProvider Provider);
