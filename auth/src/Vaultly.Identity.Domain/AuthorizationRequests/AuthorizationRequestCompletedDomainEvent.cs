using Vaultly.Core;

namespace Vaultly.Identity.Domain.AuthorizationRequests;

/// <summary>
/// Raised when an authorization request produced an authorization code.
/// </summary>
public sealed record AuthorizationRequestCompletedDomainEvent(
    AuthorizationRequestId AuthorizationRequestId,
    ClientId ClientId,
    DateTimeOffset CompletedAt) : DomainEvent;
