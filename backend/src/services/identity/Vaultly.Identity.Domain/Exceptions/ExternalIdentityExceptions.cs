using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Exceptions;

public sealed class DuplicateExternalIdentityException : DomainException
{
    /// <summary>
    /// Creates a new duplicate external identity exception for the specified provider and subject.
    /// </summary>
    public DuplicateExternalIdentityException(string providerId, string providerUserId)
        : base($"External identity already linked for provider '{providerId}' and subject '{providerUserId}'.")
    {
    }
}