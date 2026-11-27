using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Vaultly.Gateway.Http;

public static class HealthTags
{
    /// <summary>Checks that must pass before the gateway can serve traffic.</summary>
    public const string Ready = "ready";
}

internal static class GatewayHealth
{
    public static IEndpointRouteBuilder MapGatewayHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthTags.Ready) })
            .AllowAnonymous();
        return endpoints;
    }
}
