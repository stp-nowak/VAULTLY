using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions;

public sealed class AuthCodeExpiredException : DomainException
{
    /// <summary>
    /// Creates a new auth code expired exception.
    /// </summary>
    public AuthCodeExpiredException() : base("Auth code is expired.")
    {
    }
}
