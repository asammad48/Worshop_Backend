using Application.DTOs.Auth;
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
}
