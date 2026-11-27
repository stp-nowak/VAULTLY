namespace Vaultly.Identity.Application.Security;

public interface IJwtKeyProvider
{
    /// <summary>
    /// Gets the public JWK representation for the signing key.
    /// </summary>
    JwkDto GetJwk();
}

public sealed record JwkDto(string Kty, string Use, string Alg, string Kid, string N, string E);
