using Vaultly.Identity.Application.Features.Authorization;
using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Features.Authorization.StartAuthorization;

/// <summary>
/// Command behind <c>GET /authorize</c> (authorization code flow with PKCE). Raw query values are passed as-is so the
/// handler can decide which errors may be redirected back to the client and which must not (unknown client or
/// unregistered redirect URI).
/// </summary>
/// <param name="SsoToken">Value of the auth service's SSO cookie, if the browser sent one.</param>
public sealed record StartAuthorizationCommand(
    string? ClientId,
    string? RedirectUri,
    string? ResponseType,
    string? State,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? SsoToken) : ICommand<AuthorizationResult>;
