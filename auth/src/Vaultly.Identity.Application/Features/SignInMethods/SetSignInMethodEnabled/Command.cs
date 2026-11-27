using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Features.SignInMethods.SetSignInMethodEnabled;

/// <summary>
/// An admin switches a sign-in method on or off. Returns <c>false</c> when the method is unknown.
/// </summary>
/// <param name="AdminUserId">The admin making the change, for the audit log.</param>
/// <param name="Method">Method id, e.g. <c>google</c> or <c>password</c>.</param>
public sealed record SetSignInMethodEnabledCommand(Guid AdminUserId, string Method, bool Enabled) : ICommand<bool>;
