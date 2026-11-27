namespace Vaultly.Identity.Application.Features.Authorization;

/// <summary>
/// Where to send the browser after <c>/authorize</c>: back to the client (with a code or an error) or to the login UI.
/// </summary>
public sealed record AuthorizationResult(string RedirectUrl);
