namespace Vaultly.Identity.Application.Features.Sessions.EndSession;

/// <summary>
/// Where to send the browser after the SSO session ended.
/// </summary>
public sealed record EndSessionResult(string RedirectUrl);
