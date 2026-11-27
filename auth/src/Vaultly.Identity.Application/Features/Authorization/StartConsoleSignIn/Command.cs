using Vaultly.Core.Messaging;

namespace Vaultly.Identity.Application.Features.Authorization.StartConsoleSignIn;

/// <summary>
/// Signs the browser in to the admin console: straight to the console with an active SSO session, otherwise via
/// the login page.
/// </summary>
public sealed record StartConsoleSignInCommand(string? SsoToken) : ICommand<AuthorizationResult>;
