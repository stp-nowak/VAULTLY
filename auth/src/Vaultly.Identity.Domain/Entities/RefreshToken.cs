using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Entities;

public sealed class RefreshToken
{
    /// <summary>
    /// Required by EF Core.
    /// </summary>
    private RefreshToken()
    {
    }

    public RefreshTokenId Id { get; private set; }
    public SessionId SessionId { get; private set; }
    public TokenHash TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Issues a new refresh token entity.
    /// </summary>
    internal static RefreshToken Issue(SessionId sessionId, TokenHash tokenHash, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            Id = RefreshTokenId.New(),
            SessionId = sessionId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Marks the refresh token as revoked.
    /// </summary>
    internal void Revoke(DateTimeOffset now, RefreshTokenId? replacedByTokenId)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }

    /// <summary>
    /// Determines whether the refresh token is expired.
    /// </summary>
    internal bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;
}