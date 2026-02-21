using Application.DTOs.Attachments;

namespace Application.Services.Interfaces;

public interface IAttachmentService
{
    Task<AttachmentResponse> CreateMetadataAsync(Guid actorUserId, Guid branchId, AttachmentCreateRequest req, CancellationToken ct=default);
    Task<IReadOnlyList<AttachmentResponse>> ListAsync(Guid branchId, string ownerType, Guid ownerId, CancellationToken ct=default);
    Task<PresignResponse> PresignAsync(Guid actorUserId, Guid branchId, PresignRequest req, CancellationToken ct = default);
    Task<AttachmentUploadResponse> UploadAsync(Guid actorUserId, string ownerType, Guid ownerId, string? note, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
}
