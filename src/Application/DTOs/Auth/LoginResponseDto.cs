namespace Application.DTOs.Auth;
public sealed record LoginResponseDto(string AccessToken, string RefreshToken, string Role, Guid? BranchId);
