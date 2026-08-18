using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NewsIntelligence.Application;

namespace NewsIntelligence.Api;

public sealed class TokenIssuer(IConfiguration configuration) : ITokenIssuer
{
    public (string Token, DateTimeOffset ExpiresAt) Issue(AuthUser user)
    {
        var secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is required.");
        if (secret.Length < 32) throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");
        var issuer = configuration["Jwt:Issuer"] ?? "NewsIntelligence";
        var audience = configuration["Jwt:Audience"] ?? "NewsIntelligence.Web";
        var minutes = configuration.GetValue("Jwt:AccessTokenMinutes", 30);
        var expires = DateTimeOffset.UtcNow.AddMinutes(minutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires.UtcDateTime, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
