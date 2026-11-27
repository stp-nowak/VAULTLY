using Vaultly.Core;
using Vaultly.Identity.Domain.AuthorizationRequests.Clients;

namespace Vaultly.Identity.Domain.AuthorizationRequests.Events;

/// <summary>
/// Raised when a client starts signing a user in.
/// </summary>
public sealed record AuthorizationRequestCreatedDomainEvent(
    AuthorizationRequestId AuthorizationRequestId,
    ClientId ClientId,
    DateTimeOffset ExpiresAt) : DomainEvent;
