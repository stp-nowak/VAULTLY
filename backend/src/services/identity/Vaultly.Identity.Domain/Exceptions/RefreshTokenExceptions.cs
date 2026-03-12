using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Exceptions;

public sealed class RefreshTokenNotFoundException : DomainException
{
    /// <summary>
    /// Creates a new refresh token not found exception.
    /// </summary>
    public RefreshTokenNotFoundException() : base("Refresh token is invalid.")
    {
    }
}

public sealed class RefreshTokenInvalidException : DomainException
{
    /// <summary>
    /// Creates a new refresh token invalid exception.
    /// </summary>
    public RefreshTokenInvalidException(string reason) : base($"Refresh token is invalid: {reason}.")
    {
    }
}