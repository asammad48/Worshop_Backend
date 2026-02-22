using Application.DTOs.Attachments;

namespace Application.Services.Interfaces;

public interface IAttachmentService
{
    Task<AttachmentResponse> CreateMetadataAsync(Guid actorUserId, Guid branchId, AttachmentCreateRequest req, CancellationToken ct=default);
    Task<IReadOnlyList<AttachmentResponse>> ListAsync(Guid branchId, string ownerType, Guid ownerId, CancellationToken ct=default);
    Task<AttachmentResponse> UploadAsync(Guid actorUserId, Guid? branchId, string ownerType, Guid ownerId, string? note, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken ct = default);
    Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(Guid id, CancellationToken ct = default);
}
