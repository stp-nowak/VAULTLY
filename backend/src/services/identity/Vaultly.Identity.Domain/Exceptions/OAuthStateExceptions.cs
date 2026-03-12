using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Exceptions;

/// <summary>
/// Thrown when an OAuth state can no longer be used because it has expired.
/// </summary>
public sealed class OAuthStateExpiredException : DomainException
{
    /// <summary>
    /// Creates a new OAuth state expired exception.
    /// </summary>
    public OAuthStateExpiredException() : base("OAuth state is invalid or expired.")
    {
    }
}
