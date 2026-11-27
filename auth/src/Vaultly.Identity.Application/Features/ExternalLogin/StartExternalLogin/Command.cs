using Vaultly.Core.Messaging;

namespace Vaultly.Identity.Application.Features.ExternalLogin.StartExternalLogin;

/// <summary>
/// Command that starts an external OAuth login: it persists a new <c>OAuthState</c> (state, PKCE verifier, nonce)
/// for a pending authorization request and returns the provider authorization URL. Modelled as a command, not a query, because it writes state.
/// </summary>
public sealed record StartExternalLoginCommand(string ProviderId, Guid AuthorizationRequestId) : ICommand<string>;
