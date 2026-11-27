using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions;

public sealed class RefreshTokenInvalidException : DomainException
{
    /// <summary>
    /// Creates a new refresh token invalid exception.
    /// </summary>
    public RefreshTokenInvalidException(string reason) : base($"Refresh token is invalid: {reason}.")
    {
    }
}
