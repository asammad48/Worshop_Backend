namespace Application.DTOs.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? RefType,
    Guid? RefId,
    bool IsRead,
    DateTimeOffset CreatedAt
);
