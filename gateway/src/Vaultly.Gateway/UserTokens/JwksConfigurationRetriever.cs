using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Vaultly.Gateway.UserTokens;

/// <summary>
/// Reads a bare JWKS document so <see cref="ConfigurationManager{T}"/> can cache and refresh signing keys without
/// going through OIDC discovery.
/// </summary>
public sealed class JwksConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    /// <inheritdoc />
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address,
        IDocumentRetriever retriever,
        CancellationToken cancel)
    {
        var document = await retriever.GetDocumentAsync(address, cancel);
        var configuration = new OpenIdConnectConfiguration { JsonWebKeySet = new JsonWebKeySet(document) };

        foreach (var key in configuration.JsonWebKeySet.GetSigningKeys())
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }
}
