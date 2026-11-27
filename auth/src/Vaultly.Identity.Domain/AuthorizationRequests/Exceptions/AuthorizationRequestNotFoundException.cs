using Vaultly.Core;

namespace Vaultly.Identity.Domain.AuthorizationRequests.Exceptions;

public sealed class AuthorizationRequestNotFoundException : DomainException
{
    /// <summary>
    /// Creates a new exception for an unknown authorization request id.
    /// </summary>
    public AuthorizationRequestNotFoundException() : base("Authorization request was not found.")
    {
    }
}
