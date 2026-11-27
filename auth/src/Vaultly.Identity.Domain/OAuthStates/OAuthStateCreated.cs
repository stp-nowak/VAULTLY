using Vaultly.Core;

namespace Vaultly.Identity.Domain.OAuthStates;

/// <summary>
/// Raised when a new OAuth state is created for an authorization flow.
/// </summary>
public sealed record OAuthStateCreated(
    OAuthStateId OAuthStateId,
    OAuthStateValue State,
    DateTimeOffset ExpiresAt) : DomainEvent;
