namespace Application.DTOs.Auth;
public sealed record LoginResponseDto(string AccessToken,string Role,Guid? BranchId);
