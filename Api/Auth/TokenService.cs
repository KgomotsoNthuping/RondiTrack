using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Models;
using Microsoft.IdentityModel.Tokens;

namespace Api.Auth;

public sealed class TokenService
{
    private readonly JwtOptions _options;

    public TokenService(JwtOptions options)
    {
        _options = options;
    }

    public AccessTokenResult CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;

        var expiresAt = now.AddMinutes( _options.AccessTokenMinutes);

        var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                new(JwtRegisteredClaimNames.Email, user.Email),

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes( _options.SigningKey));

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: credentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(value, expiresAt);
    }
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);