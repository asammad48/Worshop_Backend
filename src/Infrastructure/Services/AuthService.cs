using Application.DTOs.Auth;
using Application.Security;
using Application.Services.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;

    public AuthService(AppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Invalid login request", new[] { "Email and password are required." });

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email.ToLower() == email && !x.IsDeleted && x.IsActive, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Invalid credentials", 401, new[] { "Invalid email or password." });

        var token = _jwt.CreateToken(user);
        return new LoginResponseDto(token, user.Role.ToString(), user.BranchId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken ct = default)
    {
        PasswordValidator.Validate(request.NewPassword);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);
        if (user == null) throw new NotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Invalid current password", new[] { "The current password provided is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MeResponseDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted && x.IsActive, ct);
        if (user == null) throw new NotFoundException("User not found or account is disabled.");

        return new MeResponseDto(user.Id, user.Email, user.Role, user.BranchId);
    }
}
