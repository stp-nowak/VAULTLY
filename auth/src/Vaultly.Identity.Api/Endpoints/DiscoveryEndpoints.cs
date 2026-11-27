using Microsoft.Extensions.Options;

using Vaultly.Core;
using Vaultly.Identity.ApiContracts;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;

namespace Vaultly.Identity.Api.Endpoints;

/// <summary>
/// OIDC discovery: what consumers need to validate this service's tokens.
/// </summary>
public static class DiscoveryEndpoints
{
    /// <summary>
    /// Maps <c>/.well-known/openid-configuration</c> and <c>/.well-known/jwks.json</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/openid-configuration", GetConfiguration);
        app.MapGet("/.well-known/jwks.json", GetKeys);
        return app;
    }

    internal static IResult GetConfiguration(IOptions<IdentityOptions> options)
    {
        var config = options.Value;
        var baseUrl = config.PublicBaseUrl.TrimEnd('/');
        return Results.Ok(new
        {
            issuer = config.Issuer,
            authorization_endpoint = $"{baseUrl}/authorize",
            token_endpoint = $"{baseUrl}/token",
            revocation_endpoint = $"{baseUrl}/revoke",
            end_session_endpoint = $"{baseUrl}/logout",
            jwks_uri = $"{baseUrl}/.well-known/jwks.json",
            response_types_supported = new[] { "code" },
            grant_types_supported = WireNames.All<GrantType>(),
            code_challenge_methods_supported = new[] { "S256" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            scopes_supported = new[] { "openid", "email", "profile" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" }
        });
    }

    internal static IResult GetKeys(IJwtKeyProvider keyProvider) => Results.Ok(new { keys = new[] { keyProvider.GetJwk() } });
}
