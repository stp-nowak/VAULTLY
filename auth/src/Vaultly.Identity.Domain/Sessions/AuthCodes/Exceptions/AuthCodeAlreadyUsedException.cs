using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.AuthCodes.Exceptions;

public sealed class AuthCodeAlreadyUsedException : DomainException
{
    /// <summary>
    /// Creates a new auth code already used exception.
    /// </summary>
    public AuthCodeAlreadyUsedException() : base("Auth code has already been used.")
    {
    }
}
