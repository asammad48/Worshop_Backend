using Application.DTOs.Auth;
namespace Application.Services.Interfaces;
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken ct = default);
    Task<MeResponseDto> GetMeAsync(Guid userId, CancellationToken ct = default);
}
