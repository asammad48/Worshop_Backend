namespace Application.DTOs.Attachments;

public sealed record AttachmentUploadResponse(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Note,
    string? Url,
    DateTimeOffset CreatedAt
);
