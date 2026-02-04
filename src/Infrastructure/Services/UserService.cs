using Application.DTOs.Users;
using Application.Pagination;
using Application.Security;
using Application.Services.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserDto> CreateAsync(CreateUserDto request, CancellationToken ct = default)
    {
        PasswordValidator.Validate(request.Password);

        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email.ToLower() == email && !x.IsDeleted, ct))
            throw new ValidationException("Email already exists", new[] { "A user with this email already exists." });

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            BranchId = request.BranchId,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(user.Id, ct);
    }

    public async Task<PageResponse<UserDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x => x.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new UserDto(
                x.Id,
                x.Email,
                x.Role,
                x.BranchId,
                x.Branch != null ? x.Branch.Name : null,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync(ct);

        return new PageResponse<UserDto>(items, total, request.PageNumber, request.PageSize);
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (user == null)
            throw new NotFoundException("User not found");

        return new UserDto(
            user.Id,
            user.Email,
            user.Role,
            user.BranchId,
            user.Branch?.Name,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        );
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (user == null) throw new NotFoundException("User not found");

        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        if (user.Email.ToLower() != email && await _db.Users.AnyAsync(x => x.Email.ToLower() == email && x.Id != id && !x.IsDeleted, ct))
            throw new ValidationException("Email already exists", new[] { "A user with this email already exists." });

        user.Email = email;
        user.BranchId = request.BranchId;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task SetPasswordAsync(Guid id, ResetPasswordDto request, CancellationToken ct = default)
    {
        PasswordValidator.Validate(request.NewPassword);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (user == null) throw new NotFoundException("User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DisableAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (user == null) throw new NotFoundException("User not found");

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task EnableAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (user == null) throw new NotFoundException("User not found");

        user.IsActive = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRoleAsync(Guid id, UpdateRoleDto request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (user == null) throw new NotFoundException("User not found");

        user.Role = request.Role;
        await _db.SaveChangesAsync(ct);
    }
}
