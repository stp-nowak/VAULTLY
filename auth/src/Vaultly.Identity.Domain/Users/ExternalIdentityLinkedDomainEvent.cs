using Vaultly.Core;
using Vaultly.Identity.Domain.SignInMethods;

namespace Vaultly.Identity.Domain.Users;

/// <summary>
/// Raised when a user links an external identity.
/// </summary>
public sealed record ExternalIdentityLinkedDomainEvent(
    UserId UserId,
    ExternalIdentityId ExternalIdentityId,
    ProviderId ProviderId,
    ProviderUserId ProviderUserId) : DomainEvent;
