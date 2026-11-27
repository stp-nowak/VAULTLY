using Vaultly.Core;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Aggregates;

public sealed class OAuthState : AggregateRoot
{
    private OAuthState()
    {
    }

    public OAuthStateId Id { get; private set; }
    public OAuthStateValue State { get; private set; } = null!;
    public CodeVerifier CodeVerifier { get; private set; } = null!;
    public Nonce Nonce { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Authorization request this external login continues.</summary>
    public AuthorizationRequestId AuthorizationRequestId { get; private set; }

    /// <summary>
    /// Creates a new OAuth state aggregate.
    /// </summary>
    public static OAuthState Create(
        OAuthStateValue state,
        CodeVerifier codeVerifier,
        Nonce nonce,
        DateTimeOffset expiresAt,
        AuthorizationRequestId authorizationRequestId)
    {
        var oauthState = new OAuthState
        {
            Id = OAuthStateId.New(),
            State = state,
            CodeVerifier = codeVerifier,
            Nonce = nonce,
            ExpiresAt = expiresAt,
            AuthorizationRequestId = authorizationRequestId
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