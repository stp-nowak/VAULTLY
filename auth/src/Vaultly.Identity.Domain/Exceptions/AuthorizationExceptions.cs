using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Domain.Exceptions;

public sealed class AuthorizationRequestExpiredException : DomainException
{
    /// <summary>
    /// Creates a new exception for an authorization request that timed out before the user signed in.
    /// </summary>
    public AuthorizationRequestExpiredException() : base("Authorization request has expired.")
    {
    }
}

public sealed class AuthorizationRequestCompletedException : DomainException
{
    /// <summary>
    /// Creates a new exception for an authorization request that was already used to issue a code.
    /// </summary>
    public AuthorizationRequestCompletedException() : base("Authorization request has already been completed.")
    {
    }
}

public sealed class AuthCodeMismatchException : DomainException
{
    /// <summary>
    /// Creates a new exception for an auth code redeemed by a different client, redirect URI or PKCE verifier
    /// than it was issued for.
    /// </summary>
    public AuthCodeMismatchException(string reason) : base($"Auth code does not match the request ({reason}).")
    {
        Reason = reason;
    }

    /// <summary>Which binding failed: <c>client</c>, <c>redirect_uri</c> or <c>code_verifier</c>.</summary>
    public string Reason { get; }
}

public sealed class AuthorizationRequestNotFoundException : DomainException
{
    /// <summary>
    /// Creates a new exception for an unknown authorization request id.
    /// </summary>
    public AuthorizationRequestNotFoundException() : base("Authorization request was not found.")
    {
    }
}
