using Vaultly.Core;

namespace Vaultly.Identity.Domain.AuthorizationRequests;

public sealed class AuthorizationRequestExpiredException : DomainException
{
    /// <summary>
    /// Creates a new exception for an authorization request that timed out before the user signed in.
    /// </summary>
    public AuthorizationRequestExpiredException() : base("Authorization request has expired.")
    {
    }
}
