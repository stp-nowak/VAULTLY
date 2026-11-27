using System.Security.Claims;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Vaultly.Identity.PublicTypes;

namespace Vaultly.Gateway.InternalTokens;

/// <summary>
/// Turns a validated user into a short-lived token for one service: audience = that service, only the claims
/// services need, signed with the gateway's key.
/// </summary>
public sealed class InternalTokenIssuer(
    IOptions<InternalTokenOptions> options,
    InternalTokenSigningKey signingKey,
    TimeProvider timeProvider)
{
    private static readonly string[] ForwardedClaims =
    [
        IdentityClaims.UserId,
        IdentityClaims.SessionId,
        IdentityClaims.Role
    ];

    private readonly JsonWebTokenHandler _handler = new();
    private readonly SigningCredentials _credentials = new(signingKey.Key, SecurityAlgorithms.RsaSha256);

    public string Issue(ClaimsPrincipal user, string audience)
    {
        var now = timeProvider.GetUtcNow();
        var expires = Earlier(now.AddMinutes(options.Value.LifetimeMinutes), UserTokenExpiry(user));

        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Value.Issuer,
            Audience = audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Claims = ForwardedClaims
                .Select(type => user.FindFirst(type))
                .OfType<Claim>()
                .ToDictionary(claim => claim.Type, object (claim) => claim.Value),
            SigningCredentials = _credentials
        });
    }

    private static DateTimeOffset? UserTokenExpiry(ClaimsPrincipal user)
        => long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Exp), out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

    private static DateTimeOffset Earlier(DateTimeOffset value, DateTimeOffset? other)
        => other is { } limit && limit < value ? limit : value;
}
