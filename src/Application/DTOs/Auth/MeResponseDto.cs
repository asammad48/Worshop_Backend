using Domain.Enums;

namespace Application.DTOs.Auth;

public sealed record MeResponseDto(
    Guid Id,
    string Email,
    UserRole Role,
    Guid? BranchId
);
