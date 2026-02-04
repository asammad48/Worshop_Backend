namespace Application.DTOs.Users;

public sealed record UpdateUserDto(
    string Email,
    Guid? BranchId
);
