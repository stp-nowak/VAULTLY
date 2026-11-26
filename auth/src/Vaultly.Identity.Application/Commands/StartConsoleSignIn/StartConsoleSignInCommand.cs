using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Commands.StartConsoleSignIn;

/// <summary>
/// Signs the browser in to the admin console: straight to the console with an active SSO session, otherwise via
/// the login page.
/// </summary>
public sealed record StartConsoleSignInCommand(string? SsoToken) : ICommand<AuthorizationResult>;
