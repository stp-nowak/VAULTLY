using Vaultly.Core;

namespace Vaultly.Identity.Domain.AuthorizationRequests;

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
