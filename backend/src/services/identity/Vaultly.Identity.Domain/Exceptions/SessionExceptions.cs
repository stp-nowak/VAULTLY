using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Exceptions;

public sealed class SessionRevokedException : DomainException
{
    /// <summary>
    /// Creates a new session revoked exception.
    /// </summary>
    public SessionRevokedException() : base("Session is revoked.")
    {
    }
}