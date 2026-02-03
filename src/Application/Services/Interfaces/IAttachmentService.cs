using Application.DTOs.Attachments;

namespace Application.Services.Interfaces;

public interface IAttachmentService
{
    Task<AttachmentResponse> CreateMetadataAsync(Guid actorUserId, Guid branchId, AttachmentCreateRequest req, CancellationToken ct=default);
    Task<IReadOnlyList<AttachmentResponse>> ListAsync(Guid branchId, string ownerType, Guid ownerId, CancellationToken ct=default);
}
