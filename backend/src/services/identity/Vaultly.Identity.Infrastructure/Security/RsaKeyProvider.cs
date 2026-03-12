using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

namespace Vaultly.Identity.Infrastructure.Security;

public sealed class RsaKeyProvider
{
    private readonly RSA _rsa;

    public RsaKeyProvider(string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            throw new InvalidOperationException("privateKeyPem is required.");
        }

        _rsa = RSA.Create();
        try
        {
            _rsa.ImportFromPem(privateKeyPem);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "The configured JWT private key is not a valid RSA private key in PEM format.",
                ex);
        }

        SecurityKey = new RsaSecurityKey(_rsa)
        {
            KeyId = ComputeKeyId(_rsa)
        };
    }

    public RsaSecurityKey SecurityKey { get; }

    public JsonWebKey GetJwk()
    {
        var parameters = _rsa.ExportParameters(false);
        return new JsonWebKey
        {
            Kty = "RSA",
            Use = "sig",
            Alg = "RS256",
            Kid = SecurityKey.KeyId,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent)
        };
    }

    private static string ComputeKeyId(RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);
        var data = new byte[parameters.Modulus!.Length + parameters.Exponent!.Length];
        Buffer.BlockCopy(parameters.Modulus, 0, data, 0, parameters.Modulus.Length);
        Buffer.BlockCopy(parameters.Exponent, 0, data, parameters.Modulus.Length, parameters.Exponent.Length);
        var hash = SHA256.HashData(data);
        return Base64UrlEncoder.Encode(hash);
    }
}