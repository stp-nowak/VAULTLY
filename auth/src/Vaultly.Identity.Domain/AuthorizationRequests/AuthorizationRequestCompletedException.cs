using Vaultly.Core;

namespace Vaultly.Identity.Domain.AuthorizationRequests;

public sealed class AuthorizationRequestCompletedException : DomainException
{
    /// <summary>
    /// Creates a new exception for an authorization request that was already used to issue a code.
    /// </summary>
    public AuthorizationRequestCompletedException() : base("Authorization request has already been completed.")
    {
    }
}
