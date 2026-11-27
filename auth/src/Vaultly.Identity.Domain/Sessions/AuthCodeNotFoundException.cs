using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions;

public sealed class AuthCodeNotFoundException : DomainException
{
    /// <summary>
    /// Creates a new auth code not found exception.
    /// </summary>
    public AuthCodeNotFoundException() : base("Auth code is invalid.")
    {
    }
}
