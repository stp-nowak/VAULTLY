using Vaultly.Identity.Application.Interfaces.Services;

namespace Vaultly.Identity.Infrastructure.Security;

/// <summary>
/// Provides public JWK metadata for the RSA signing key.
/// </summary>
public sealed class JwtKeyProvider(RsaKeyProvider rsaKeyProvider) : IJwtKeyProvider
{
    private readonly RsaKeyProvider _rsaKeyProvider = rsaKeyProvider;

    /// <summary>
    /// Gets the public JWK representation for the signing key.
    /// </summary>
    public JwkDto GetJwk()
    {
        var jwk = _rsaKeyProvider.GetJwk();
        return new JwkDto(jwk.Kty, jwk.Use, jwk.Alg, jwk.Kid, jwk.N, jwk.E);
    }
}