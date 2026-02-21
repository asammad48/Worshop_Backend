using Api.Security;
using Application.DTOs.Attachments;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/attachments")]
[Authorize(Policy = "BranchUser")]
public sealed class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _svc;
    public AttachmentsController(IAttachmentService svc) { _svc = svc; }

    [HttpPost("metadata")]
    public async Task<ActionResult<ApiResponse<AttachmentResponse>>> CreateMetadata([FromBody] AttachmentCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<AttachmentResponse>.Ok(await _svc.CreateMetadataAsync(userId, branchId, req, ct));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AttachmentResponse>>>> List([FromQuery] string ownerType, [FromQuery] Guid ownerId, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<AttachmentResponse>>.Ok(await _svc.ListAsync(branchId, ownerType, ownerId, ct));
    }

    [HttpPost("presign")]
    public async Task<ActionResult<ApiResponse<PresignResponse>>> Presign([FromBody] PresignRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<PresignResponse>.Ok(await _svc.PresignAsync(userId, branchId, req, ct));
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<AttachmentUploadResponse>>> Upload(
        [FromForm] string ownerType,
        [FromForm] Guid ownerId,
        [FromForm] string? note,
        IFormFile file,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        using var stream = file.OpenReadStream();
        var res = await _svc.UploadAsync(userId, ownerType, ownerId, note, stream, file.FileName, file.ContentType, ct);
        return Ok(ApiResponse<AttachmentUploadResponse>.Ok(res));
    }
}
