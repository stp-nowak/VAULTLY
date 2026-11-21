using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Domain.Aggregates;

/// <summary>
/// A client's pending request to sign a user in (OAuth 2.0 authorization code + PKCE). It remembers where to send
/// the user back and with which state/challenge while the user signs in on the auth service's own UI.
/// </summary>
public sealed class AuthorizationRequest : AggregateRoot
{
    private AuthorizationRequest()
    {
    }

    public AuthorizationRequestId Id { get; private set; }
    public ClientId ClientId { get; private set; } = null!;
    public RedirectUri RedirectUri { get; private set; } = null!;
    public ClientState State { get; private set; } = null!;
    public CodeChallenge CodeChallenge { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Whether the request has already produced an authorization code.</summary>
    public bool IsCompleted => CompletedAt.HasValue;

    /// <summary>
    /// Starts a new authorization request. The caller must have validated the client and redirect URI.
    /// </summary>
    public static AuthorizationRequest Create(
        ClientId clientId,
        RedirectUri redirectUri,
        ClientState state,
        CodeChallenge codeChallenge,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (expiresAt <= now)
        {
            throw new InvalidValueException("Authorization request must expire in the future.");
        }

        var request = new AuthorizationRequest
        {
            Id = AuthorizationRequestId.New(),
            ClientId = clientId,
            RedirectUri = redirectUri,
            State = state,
            CodeChallenge = codeChallenge,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };

        request.RaiseDomainEvent(new AuthorizationRequestCreatedDomainEvent(request.Id, clientId, expiresAt));
        return request;
    }

    /// <summary>Whether the request expired before completion.</summary>
    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    /// <summary>
    /// Ensures the request can still be continued (not expired, not completed).
    /// </summary>
    public void EnsurePending(DateTimeOffset now)
    {
        if (IsCompleted)
        {
            throw new AuthorizationRequestCompletedException();
        }

        if (IsExpired(now))
        {
            throw new AuthorizationRequestExpiredException();
        }
    }

    /// <summary>
    /// Marks the request as completed once a code has been issued; a request can be completed only once.
    /// </summary>
    public void Complete(DateTimeOffset now)
    {
        EnsurePending(now);
        CompletedAt = now;
        RaiseDomainEvent(new AuthorizationRequestCompletedDomainEvent(Id, ClientId, now));
    }
}
