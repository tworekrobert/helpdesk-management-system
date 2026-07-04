using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Helpdesk.Application.Auth;
using Helpdesk.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.Api.Auth;

public sealed class JwtTokenFactory(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public AuthenticatedUserResponse Create(AppUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiresMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthenticatedUserResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            new CurrentUserResponse(user.Id, user.FullName, user.Email, user.Role, user.Department));
    }
}
