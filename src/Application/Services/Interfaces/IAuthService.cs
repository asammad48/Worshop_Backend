using Application.DTOs.Auth;
namespace Application.Services.Interfaces;
public interface IAuthService { Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct=default); }
