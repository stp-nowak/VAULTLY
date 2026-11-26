namespace Vaultly.Identity.Api.Endpoints;

/// <summary>
/// Every endpoint of the identity service.
/// </summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
        => app
            .MapOAuthEndpoints()
            .MapPasswordEndpoints()
            .MapAdminEndpoints()
            .MapSessionEndpoints()
            .MapDiscoveryEndpoints();
}
