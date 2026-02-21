using Api.Security;
using Application.DTOs.Attachments;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<AttachmentResponse>>> Upload(
        [FromForm] string ownerType,
        [FromForm] Guid ownerId,
        [FromForm] string? note,
        IFormFile file,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        Guid? branchId = null;
        var branchClaim = User.FindFirst("branchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var bId)) branchId = bId;

        using var stream = file.OpenReadStream();
        var result = await _svc.UploadAsync(userId, branchId, ownerType, ownerId, note, file.FileName, file.ContentType, file.Length, stream, ct);
        return ApiResponse<AttachmentResponse>.Ok(result);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (stream, fileName, contentType) = await _svc.DownloadAsync(id, ct);
        return File(stream, contentType, fileName);
    }
}
