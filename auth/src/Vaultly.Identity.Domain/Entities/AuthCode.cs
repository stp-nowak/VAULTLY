using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Entities;

public sealed class AuthCode
{
    private AuthCode()
    {
    }

    public AuthCodeId Id { get; private set; }
    public SessionId SessionId { get; private set; }
    public CodeHash CodeHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>Client the code was issued to; only that client can redeem it.</summary>
    public ClientId ClientId { get; private set; } = null!;

    /// <summary>Redirect URI of the authorization request; must be repeated verbatim at the token endpoint.</summary>
    public RedirectUri RedirectUri { get; private set; } = null!;

    /// <summary>PKCE challenge the redeeming client must satisfy with its code verifier.</summary>
    public CodeChallenge CodeChallenge { get; private set; } = null!;

    public bool IsUsed => UsedAt.HasValue;

    internal static AuthCode Issue(
        SessionId sessionId,
        CodeHash codeHash,
        DateTimeOffset expiresAt,
        ClientId clientId,
        RedirectUri redirectUri,
        CodeChallenge codeChallenge)
    {
        return new AuthCode
        {
            Id = AuthCodeId.New(),
            SessionId = sessionId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            ClientId = clientId,
            RedirectUri = redirectUri,
            CodeChallenge = codeChallenge
        };
    }

    internal void MarkUsed(DateTimeOffset now)
    {
        if (UsedAt is null)
        {
            UsedAt = now;
        }
    }

    internal bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;
}