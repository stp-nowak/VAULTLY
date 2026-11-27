using Vaultly.Core;
using Vaultly.Identity.Domain.AuthorizationRequests.Clients;
using Vaultly.Identity.Domain.AuthorizationRequests.Events;
using Vaultly.Identity.Domain.AuthorizationRequests.Exceptions;

namespace Vaultly.Identity.Domain.AuthorizationRequests;

/// <summary>
/// A pending request to sign a user in while the user is on the auth service's own UI. For a client it is an
/// OAuth 2.0 authorization code + PKCE request: it remembers where to send the user back and with which
/// state/challenge. For the admin console it only remembers the console URL.
/// </summary>
public sealed class AuthorizationRequest : AggregateRoot
{
    private AuthorizationRequest()
    {
    }

    public AuthorizationRequestId Id { get; private set; }
    public ClientId ClientId { get; private set; } = null!;
    public RedirectUri RedirectUri { get; private set; } = null!;

    /// <summary>Client state to echo back with the code; <c>null</c> for the admin console.</summary>
    public ClientState? State { get; private set; }

    /// <summary>PKCE challenge the code is bound to; <c>null</c> for the admin console.</summary>
    public CodeChallenge? CodeChallenge { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Whether the user has already signed in for this request.</summary>
    public bool IsCompleted => CompletedAt.HasValue;

    /// <summary>Whether the user is signing in to the admin console rather than to a client.</summary>
    public bool IsForConsole => ClientId == ConsoleClient.Id;

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
        if (clientId == ConsoleClient.Id)
        {
            throw new InvalidValueException("The admin console is not an OAuth client.");
        }

        return Start(clientId, redirectUri, state, codeChallenge, now, expiresAt);
    }

    /// <summary>
    /// Starts signing a user in to the admin console; after sign-in the browser returns to <paramref name="consoleUrl"/>.
    /// </summary>
    public static AuthorizationRequest CreateForConsole(RedirectUri consoleUrl, DateTimeOffset now, DateTimeOffset expiresAt)
        => Start(ConsoleClient.Id, consoleUrl, state: null, codeChallenge: null, now, expiresAt);

    private static AuthorizationRequest Start(
        ClientId clientId,
        RedirectUri redirectUri,
        ClientState? state,
        CodeChallenge? codeChallenge,
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
    /// Marks the request as completed once the user has signed in; a request can be completed only once.
    /// </summary>
    public void Complete(DateTimeOffset now)
    {
        EnsurePending(now);
        CompletedAt = now;
        RaiseDomainEvent(new AuthorizationRequestCompletedDomainEvent(Id, ClientId, now));
    }
}
