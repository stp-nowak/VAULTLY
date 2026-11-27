using Vaultly.Core;
using Vaultly.Identity.Domain.Entities;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Aggregates;

public sealed class Session : AggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<AuthCode> _authCodes = new();

    private Session()
    {
    }

    public SessionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUsedAt { get; private set; }
    public UserAgent? UserAgent { get; private set; }
    public IpAddress? IpAddress { get; private set; }
    public AuthMethod AuthMethod { get; private set; } = null!;
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Hash of the single sign-on token kept in the auth service's own cookie. While the SSO session is active,
    /// further authorization requests are answered without showing the login page again.
    /// </summary>
    public TokenHash SsoTokenHash { get; private set; } = null!;

    /// <summary>Absolute end of the SSO session; afterwards the user must sign in again.</summary>
    public DateTimeOffset SsoExpiresAt { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<AuthCode> AuthCodes => _authCodes.AsReadOnly();

    /// <summary>
    /// Creates a new session aggregate.
    /// </summary>
    public static Session Create(
        UserId userId,
        AuthMethod authMethod,
        UserAgent? userAgent,
        IpAddress? ipAddress,
        TokenHash ssoTokenHash,
        DateTimeOffset now,
        DateTimeOffset ssoExpiresAt)
    {
        if (ssoExpiresAt <= now)
        {
            throw new InvalidValueException("SSO session must expire in the future.");
        }

        var session = new Session
        {
            Id = SessionId.New(),
            UserId = userId,
            CreatedAt = now,
            LastUsedAt = now,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            AuthMethod = authMethod,
            IsRevoked = false,
            SsoTokenHash = ssoTokenHash,
            SsoExpiresAt = ssoExpiresAt
        };

        session.RaiseDomainEvent(new SessionStartedDomainEvent(session.Id, session.UserId, session.AuthMethod));
        return session;
    }

    /// <summary>
    /// Whether the SSO session can still sign the user in silently.
    /// </summary>
    public bool IsSsoActive(DateTimeOffset now) => !IsRevoked && SsoExpiresAt > now;

    /// <summary>
    /// Marks the session as recently used.
    /// </summary>
    public void MarkUsed(DateTimeOffset now)
    {
        LastUsedAt = now;
    }

    /// <summary>
    /// Revokes the session if it is still active.
    /// </summary>
    public void Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
        {
            return;
        }

        IsRevoked = true;
        RevokedAt = now;
        RaiseDomainEvent(new SessionRevokedDomainEvent(Id, UserId));
    }

    /// <summary>
    /// Revokes the session and all refresh tokens.
    /// </summary>
    public void RevokeWithTokens(DateTimeOffset now)
    {
        Revoke(now);

        foreach (var token in _refreshTokens)
        {
            if (!token.IsRevoked)
            {
                token.Revoke(now, null);
            }
        }
    }

    /// <summary>
    /// Issues a new auth code bound to the client, redirect URI and PKCE challenge of an authorization request.
    /// </summary>
    public AuthCode IssueAuthCode(
        CodeHash codeHash,
        DateTimeOffset expiresAt,
        ClientId clientId,
        RedirectUri redirectUri,
        CodeChallenge codeChallenge)
    {
        EnsureNotRevoked();

        var authCode = AuthCode.Issue(Id, codeHash, expiresAt, clientId, redirectUri, codeChallenge);
        _authCodes.Add(authCode);
        RaiseDomainEvent(new SessionAuthCodeIssuedDomainEvent(Id, authCode.Id, authCode.ExpiresAt));
        return authCode;
    }

    /// <summary>
    /// Consumes an auth code if it is valid for this client, redirect URI and PKCE verifier, and marks the session
    /// as used. The code is burned even when a binding check fails, so a stolen code cannot be retried.
    /// </summary>
    public AuthCode ConsumeAuthCode(
        CodeHash codeHash,
        ClientId clientId,
        RedirectUri redirectUri,
        string? codeVerifier,
        DateTimeOffset now)
    {
        EnsureNotRevoked();

        var authCode = _authCodes.SingleOrDefault(x => x.CodeHash == codeHash);
        if (authCode is null)
        {
            throw new AuthCodeNotFoundException();
        }

        if (authCode.IsExpired(now))
        {
            throw new AuthCodeExpiredException();
        }

        if (authCode.IsUsed)
        {
            throw new AuthCodeAlreadyUsedException();
        }

        authCode.MarkUsed(now);

        if (authCode.ClientId != clientId)
        {
            throw new AuthCodeMismatchException("client");
        }

        if (authCode.RedirectUri != redirectUri)
        {
            throw new AuthCodeMismatchException("redirect_uri");
        }

        if (!authCode.CodeChallenge.IsSatisfiedBy(codeVerifier))
        {
            throw new AuthCodeMismatchException("code_verifier");
        }

        MarkUsed(now);
        RaiseDomainEvent(new SessionAuthCodeConsumedDomainEvent(Id, authCode.Id, now));
        return authCode;
    }

    /// <summary>
    /// Issues a new refresh token for the session.
    /// </summary>
    public RefreshToken IssueRefreshToken(TokenHash tokenHash, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        EnsureNotRevoked();

        var token = RefreshToken.Issue(Id, tokenHash, now, expiresAt);
        _refreshTokens.Add(token);
        RaiseDomainEvent(new SessionRefreshTokenIssuedDomainEvent(Id, token.Id, token.ExpiresAt));
        return token;
    }

    /// <summary>
    /// Rotates an existing refresh token and marks the session as used.
    /// </summary>
    public RefreshToken RotateRefreshToken(TokenHash existingTokenHash, TokenHash newTokenHash, DateTimeOffset now, DateTimeOffset newExpiresAt)
    {
        EnsureNotRevoked();

        var existing = _refreshTokens.SingleOrDefault(x => x.TokenHash == existingTokenHash);
        if (existing is null)
        {
            throw new RefreshTokenNotFoundException();
        }

        if (existing.IsRevoked)
        {
            throw new RefreshTokenInvalidException("revoked");
        }

        if (existing.IsExpired(now))
        {
            throw new RefreshTokenInvalidException("expired");
        }

        var rotated = RefreshToken.Issue(Id, newTokenHash, now, newExpiresAt);
        existing.Revoke(now, rotated.Id);
        _refreshTokens.Add(rotated);
        MarkUsed(now);
        RaiseDomainEvent(new SessionRefreshTokenRotatedDomainEvent(Id, existing.Id, rotated.Id, rotated.ExpiresAt));
        return rotated;
    }

    private void EnsureNotRevoked()
    {
        if (IsRevoked)
        {
            throw new SessionRevokedException();
        }
    }
}