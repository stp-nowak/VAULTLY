using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Vaultly.Accounts.Api.Http;

public static class AccountsHealth
{
    /// <summary>Checks that must pass before the service can serve traffic.</summary>
    public const string Ready = "ready";

    public static IEndpointRouteBuilder MapAccountsHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(Ready) })
            .AllowAnonymous();
        return endpoints;
    }
}
