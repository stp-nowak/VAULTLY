using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Exceptions;

public sealed class AuthCodeNotFoundException : DomainException
{
    /// <summary>
    /// Creates a new auth code not found exception.
    /// </summary>
    public AuthCodeNotFoundException() : base("Auth code is invalid.")
    {
    }
}

public sealed class AuthCodeExpiredException : DomainException
{
    /// <summary>
    /// Creates a new auth code expired exception.
    /// </summary>
    public AuthCodeExpiredException() : base("Auth code is expired.")
    {
    }
}

public sealed class AuthCodeAlreadyUsedException : DomainException
{
    /// <summary>
    /// Creates a new auth code already used exception.
    /// </summary>
    public AuthCodeAlreadyUsedException() : base("Auth code has already been used.")
    {
    }
}