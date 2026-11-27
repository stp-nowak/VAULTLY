using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Features.Passwords.SignInWithPassword;

/// <summary>
/// Signs a user in with email and password for a pending authorization request. Throws
/// <see cref="Common.PasswordSignInException"/> for wrong credentials (also for an unknown email or a paused account).
/// </summary>
public sealed record SignInWithPasswordCommand(
    Guid AuthorizationRequestId,
    string Email,
    string Password,
    string? UserAgent,
    string? IpAddress) : ICommand<AuthCallbackResult>
{
    public override string ToString() => $"{nameof(SignInWithPasswordCommand)} {{ AuthorizationRequestId = {AuthorizationRequestId} }}";
}
