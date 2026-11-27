namespace Vaultly.Identity.Domain.AuthorizationRequests;

/// <summary>
/// The Identity service's own admin console. It is not an OAuth client: signing in to it only starts an SSO
/// session, no authorization code is issued.
/// </summary>
public static class ConsoleClient
{
    public static readonly ClientId Id = new("vaultly-admin-console");
}
