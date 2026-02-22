using Application.DTOs.Notifications;
using Application.Pagination;

namespace Application.Services.Interfaces;

public interface INotificationService
{
    Task<PageResponse<NotificationResponse>> GetNotificationsAsync(Guid? userId, Guid? branchId, bool unreadOnly, string? type, int pageNumber, int pageSize);
    Task<bool> MarkAsReadAsync(Guid id, Guid userId);
    Task CreateNotificationAsync(string type, string title, string message, string? refType, Guid? refId, Guid? userId, Guid? branchId);
}
