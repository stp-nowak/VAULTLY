using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Entities;

public sealed class AuthCode
{
    /// <summary>
    /// Required by EF Core.
    /// </summary>
    private AuthCode()
    {
    }

    public AuthCodeId Id { get; private set; }
    public SessionId SessionId { get; private set; }
    public CodeHash CodeHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsUsed => UsedAt.HasValue;

    /// <summary>
    /// Issues a new auth code entity.
    /// </summary>
    internal static AuthCode Issue(SessionId sessionId, CodeHash codeHash, DateTimeOffset expiresAt)
    {
        return new AuthCode
        {
            Id = AuthCodeId.New(),
            SessionId = sessionId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Marks the auth code as used.
    /// </summary>
    internal void MarkUsed(DateTimeOffset now)
    {
        if (UsedAt is null)
        {
            UsedAt = now;
        }
    }

    /// <summary>
    /// Determines whether the auth code is expired.
    /// </summary>
    internal bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;
}