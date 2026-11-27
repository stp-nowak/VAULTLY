using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Features.Passwords.RegisterWithPassword;

/// <summary>
/// Creates an account with email and password for a pending authorization request and signs it in. The email stays
/// unverified. Throws <see cref="Common.PasswordSignInException"/> when the email is already used.
/// </summary>
public sealed record RegisterWithPasswordCommand(
    Guid AuthorizationRequestId,
    string Name,
    string Email,
    string Password,
    string? UserAgent,
    string? IpAddress) : ICommand<AuthCallbackResult>
{
    public override string ToString() => $"{nameof(RegisterWithPasswordCommand)} {{ AuthorizationRequestId = {AuthorizationRequestId} }}";
}
