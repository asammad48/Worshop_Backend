using Api.Security;
using Application.DTOs.Notifications;
using Application.Pagination;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers.v1;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<NotificationResponse>>>> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] string? type = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        Guid? branchId = null;
        try { branchId = User.GetBranchIdOrThrow(); } catch { }
        return ApiResponse<PageResponse<NotificationResponse>>.Ok(await _notifications.GetNotificationsAsync(userId, branchId, unreadOnly, type, pageNumber, pageSize));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
    {
        var userId = User.GetUserId();
        return ApiResponse<bool>.Ok(await _notifications.MarkAsReadAsync(id, userId));
    }
}
