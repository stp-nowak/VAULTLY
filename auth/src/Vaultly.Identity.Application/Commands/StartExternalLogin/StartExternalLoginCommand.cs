using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Commands.StartExternalLogin;

/// <summary>
/// Command that starts an external OAuth login: it persists a new <c>OAuthState</c> (state, PKCE verifier, nonce)
/// and returns the provider authorization URL. Modelled as a command, not a query, because it writes state.
/// </summary>
public sealed record StartExternalLoginCommand(string ProviderId, string? RedirectUri) : ICommand<string>;
