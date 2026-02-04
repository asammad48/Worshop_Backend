using Application.DTOs.Users;
using Application.Pagination;

namespace Application.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserDto request, CancellationToken ct = default);
    Task<PageResponse<UserDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto request, CancellationToken ct = default);
    Task SetPasswordAsync(Guid id, ResetPasswordDto request, CancellationToken ct = default);
    Task DisableAsync(Guid id, CancellationToken ct = default);
    Task EnableAsync(Guid id, CancellationToken ct = default);
    Task UpdateRoleAsync(Guid id, UpdateRoleDto request, CancellationToken ct = default);
}
