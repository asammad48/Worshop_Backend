using Domain.Enums;

namespace Application.DTOs.Users;

public sealed record CreateUserDto(
    string Email,
    string Password,
    UserRole Role,
    Guid? BranchId
);
