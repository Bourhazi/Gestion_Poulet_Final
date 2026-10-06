using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Poulet.Domain;

namespace Poulet.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "Poulet.Api";
    public string Audience { get; init; } = "Poulet.Web";
    public string Secret { get; init; } = "";
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions settings = options.Value;

    public string CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username), new(ClaimTypes.Role, user.Role), new("stamp", SessionStamp.Create(user.PasswordHash))
        };
        if (user.ClientId.HasValue) claims.Add(new("client_id", user.ClientId.Value.ToString()));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.Add(AccessLifetime),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
    }

    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    public string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public TimeSpan AccessLifetime => TimeSpan.FromMinutes(settings.AccessTokenMinutes);
    public TimeSpan RefreshLifetime => TimeSpan.FromDays(settings.RefreshTokenDays);
    public DateTimeOffset RefreshExpiry => DateTimeOffset.UtcNow.Add(RefreshLifetime);
}
