using Application.DTOs.Notifications;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PageResponse<NotificationResponse>> GetNotificationsAsync(Guid? userId, Guid? branchId, bool unreadOnly, string? type, int pageNumber, int pageSize)
    {
        var query = _db.Notifications.AsNoTracking();

        if (userId.HasValue && branchId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value || (x.UserId == null && x.BranchId == branchId.Value));
        }
        else if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }
        else if (branchId.HasValue)
        {
            query = query.Where(x => x.BranchId == branchId.Value);
        }

        if (unreadOnly)
            query = query.Where(x => !x.IsRead);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(x => x.Type == type);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationResponse(
                x.Id,
                x.Type,
                x.Title,
                x.Message,
                x.RefType,
                x.RefId,
                x.IsRead,
                x.CreatedAt
            ))
            .ToListAsync();

        return new PageResponse<NotificationResponse>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> MarkAsReadAsync(Guid id, Guid userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (notification == null) return false;

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task CreateNotificationAsync(string type, string title, string message, string? refType, Guid? refId, Guid? userId, Guid? branchId)
    {
        var notification = new Notification
        {
            Type = type,
            Title = title,
            Message = message,
            RefType = refType,
            RefId = refId,
            UserId = userId,
            BranchId = branchId,
            IsRead = false
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }
}
