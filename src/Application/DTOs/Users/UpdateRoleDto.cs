using Domain.Enums;

namespace Application.DTOs.Users;

public sealed record UpdateRoleDto(
    UserRole Role
);
