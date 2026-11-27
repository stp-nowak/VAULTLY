using Vaultly.Core;
using Vaultly.Identity.Domain.AuthorizationRequests.Clients;

namespace Vaultly.Identity.Domain.AuthorizationRequests.Events;

/// <summary>
/// Raised when an authorization request produced an authorization code.
/// </summary>
public sealed record AuthorizationRequestCompletedDomainEvent(
    AuthorizationRequestId AuthorizationRequestId,
    ClientId ClientId,
    DateTimeOffset CompletedAt) : DomainEvent;
