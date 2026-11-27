using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Sessions.RefreshTokens;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Common;

internal static class TokenResults
{
    /// <summary>
    /// Creates the access token for the user/session and packages it with the plain refresh token.
    /// </summary>
    public static TokenResult Create(
        IJwtTokenService jwtTokenService,
        IdentityOptions options,
        User user,
        Session session,
        string plainRefreshToken,
        RefreshToken refreshToken,
        DateTimeOffset now)
        => new(
            jwtTokenService.CreateAccessToken(user, session, now),
            options.AccessTokenTtlMinutes * 60,
            plainRefreshToken,
            (int)Math.Max(0, (refreshToken.ExpiresAt - now).TotalSeconds),
            session.Id.Value,
            new AuthenticatedUserDto(user.Id.Value, user.Email.Value, user.Name.Value));
}
