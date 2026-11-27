using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Vaultly.Core.Authentication;

/// <summary>
/// Reads a bare JWKS document so <see cref="ConfigurationManager{T}"/> can cache and refresh the gateway's keys.
/// </summary>
internal sealed class JwksConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
{
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
