using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Aggregates;

public sealed class OAuthState : AggregateRoot
{
    /// <summary>
    /// Required by EF Core.
    /// </summary>
    private OAuthState()
    {
    }

    public OAuthStateId Id { get; private set; }
    public OAuthStateValue State { get; private set; } = null!;
    public CodeVerifier CodeVerifier { get; private set; } = null!;
    public Nonce Nonce { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public RedirectUri? RedirectUri { get; private set; }

    /// <summary>
    /// Creates a new OAuth state aggregate.
    /// </summary>
    public static OAuthState Create(
        OAuthStateValue state,
        CodeVerifier codeVerifier,
        Nonce nonce,
        DateTimeOffset expiresAt,
        RedirectUri? redirectUri)
    {
        var oauthState = new OAuthState
        {
            Id = OAuthStateId.New(),
            State = state,
            CodeVerifier = codeVerifier,
            Nonce = nonce,
            ExpiresAt = expiresAt,
            RedirectUri = redirectUri
        };

        oauthState.RaiseDomainEvent(new OAuthStateCreatedDomainEvent(oauthState.Id, oauthState.State, oauthState.ExpiresAt));
        return oauthState;
    }

    /// <summary>
    /// Determines whether the OAuth state has expired.
    /// </summary>
    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    /// <summary>
    /// Ensures the OAuth state can still be used for the current callback flow.
    /// </summary>
    public void EnsureActive(DateTimeOffset now)
    {
        if (IsExpired(now))
        {
            throw new OAuthStateExpiredException();
        }
    }
}