using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Matchcota.Services.Auth.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matchcota.Api.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> jwtOptions) : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateToken(AuthResult authResult)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, authResult.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, authResult.Email),
            new(JwtRegisteredClaimNames.Name, authResult.DisplayName),
            new(ClaimTypes.NameIdentifier, authResult.UserId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
