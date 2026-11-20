using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Aggregates;

namespace Vaultly.Identity.Infrastructure.Security;

/// <summary>
/// JWT access token generator.
/// </summary>
public sealed class JwtTokenService(IOptions<IdentityOptions> options, RsaKeyProvider keyProvider) : IJwtTokenService
{
    private readonly IdentityOptions _options = options.Value;
    private readonly RsaKeyProvider _keyProvider = keyProvider;
    private readonly JwtSecurityTokenHandler _handler = new();

    /// <summary>
    /// Creates a signed access token for the provided user and session.
    /// </summary>
    public string CreateAccessToken(User user, Session session, DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("sid", session.Id.Value.ToString()),
            new("amr", session.AuthMethod.Value),
            new("email", user.Email.Value),
            new("name", user.Name.Value)
        };

        var credentials = new SigningCredentials(_keyProvider.SecurityKey, SecurityAlgorithms.RsaSha256);
        var expires = now.AddMinutes(_options.AccessTokenTtlMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }
}