using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

public interface IJwtTokenService { string CreateToken(User user); }

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _o;
    public JwtTokenService(IOptions<JwtOptions> o) { _o = o.Value; }

    public string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString())
        };
        if (user.BranchId is not null) claims.Add(new("branchId", user.BranchId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_o.Issuer, _o.Audience, claims, expires: DateTime.UtcNow.AddHours(12), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
