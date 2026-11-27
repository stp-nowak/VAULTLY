using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.RefreshTokens;

public sealed class RefreshTokenNotFoundException : DomainException
{
    /// <summary>
    /// Creates a new refresh token not found exception.
    /// </summary>
    public RefreshTokenNotFoundException() : base("Refresh token is invalid.")
    {
    }
}
