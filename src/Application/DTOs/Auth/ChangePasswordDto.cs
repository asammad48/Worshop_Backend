namespace Application.DTOs.Auth;

public sealed record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);
