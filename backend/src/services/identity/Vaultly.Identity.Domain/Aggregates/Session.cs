using Vaultly.Identity.Domain.Entities;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Aggregates;

public sealed class Session : AggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<AuthCode> _authCodes = new();

    /// <summary>
    /// Required by EF Core.
    /// </summary>
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

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<AuthCode> AuthCodes => _authCodes.AsReadOnly();

    /// <summary>
    /// Creates a new session aggregate.
    /// </summary>
    public static Session Create(UserId userId, AuthMethod authMethod, UserAgent? userAgent, IpAddress? ipAddress, DateTimeOffset now)
    {
        var session = new Session
        {
            Id = SessionId.New(),
            UserId = userId,
            CreatedAt = now,
            LastUsedAt = now,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            AuthMethod = authMethod,
            IsRevoked = false
        };

        session.RaiseDomainEvent(new SessionStartedDomainEvent(session.Id, session.UserId, session.AuthMethod));
        return session;
    }

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
    /// Issues a new auth code for the session.
    /// </summary>
    public AuthCode IssueAuthCode(CodeHash codeHash, DateTimeOffset expiresAt)
    {
        EnsureNotRevoked();

        var authCode = AuthCode.Issue(Id, codeHash, expiresAt);
        _authCodes.Add(authCode);
        RaiseDomainEvent(new SessionAuthCodeIssuedDomainEvent(Id, authCode.Id, authCode.ExpiresAt));
        return authCode;
    }

    /// <summary>
    /// Consumes an auth code if it is valid and marks the session as used.
    /// </summary>
    public AuthCode ConsumeAuthCode(CodeHash codeHash, DateTimeOffset now)
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

    /// <summary>
    /// Ensures the session is not revoked before performing sensitive operations.
    /// </summary>
    private void EnsureNotRevoked()
    {
        if (IsRevoked)
        {
            throw new SessionRevokedException();
        }
    }
}