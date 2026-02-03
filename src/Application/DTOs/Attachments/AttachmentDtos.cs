namespace Application.DTOs.Attachments;

public sealed record AttachmentCreateRequest(string OwnerType, Guid OwnerId, string FileName, string ContentType, long SizeBytes, string StorageKey);
public sealed record AttachmentResponse(Guid Id, string OwnerType, Guid OwnerId, string FileName, string ContentType, long SizeBytes, string StorageKey, DateTimeOffset UploadedAt, Guid UploadedByUserId);
