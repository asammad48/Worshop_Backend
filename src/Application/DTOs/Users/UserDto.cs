using Domain.Enums;

namespace Application.DTOs.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    UserRole Role,
    Guid? BranchId,
    string? BranchName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
