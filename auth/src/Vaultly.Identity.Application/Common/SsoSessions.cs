using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Common;

/// <summary>A started SSO session and the token for the browser's cookie (only its hash is stored).</summary>
internal sealed record StartedSsoSession(Session Session, string SsoToken, DateTimeOffset ExpiresAt);

internal static class SsoSessions
{
    /// <summary>Starts the SSO session of a user who just signed in.</summary>
    public static StartedSsoSession Start(
        UserId userId,
        AuthMethod authMethod,
        string? userAgent,
        string? ipAddress,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IdentityOptions options,
        DateTimeOffset now)
    {
        var ssoToken = tokenGenerator.GenerateToken(32);
        var expiresAt = now.AddHours(options.SsoSessionTtlHours);
        var session = Session.Create(
            userId,
            authMethod,
            UserAgent.FromNullable(OAuthFlowUtilities.Truncate(userAgent, 512)),
            IpAddress.FromNullable(OAuthFlowUtilities.Truncate(ipAddress, 64)),
            new TokenHash(tokenHasher.HashToken(ssoToken)),
            now,
            expiresAt);

        return new StartedSsoSession(session, ssoToken, expiresAt);
    }
}
