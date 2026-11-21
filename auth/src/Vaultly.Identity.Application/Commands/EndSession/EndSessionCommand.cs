using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Commands.EndSession;

/// <summary>
/// Browser-facing logout (<c>GET /logout</c>): ends the SSO session identified by the SSO cookie and decides where to
/// send the browser — the client's registered post-logout URI, or the login UI's signed-out page.
/// </summary>
public sealed record EndSessionCommand(string? SsoToken, string? ClientId, string? PostLogoutRedirectUri) : ICommand<EndSessionResult>;
