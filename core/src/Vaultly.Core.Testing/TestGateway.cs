using System.Net;
using System.Security.Cryptography;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Vaultly.Core.Authentication;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Core.Testing;

/// <summary>
/// Stands in for the gateway in a service's API tests: serves its signing key as JWKS and issues internal tokens
/// for the service, so tests run the real <see cref="InternalTokenAuthentication"/>.
/// </summary>
public sealed class TestGateway(string audience)
{
    public const string Issuer = "vaultly-gateway";
    public const string JwksUrl = "https://gateway.test/.well-known/jwks.json";

    private readonly RSA _key = RSA.Create(2048);

    /// <summary>The <c>InternalTokens</c> section a service under test needs.</summary>
    public IReadOnlyDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        [$"{InternalTokenOptions.SectionName}:{nameof(InternalTokenOptions.Issuer)}"] = Issuer,
        [$"{InternalTokenOptions.SectionName}:{nameof(InternalTokenOptions.Audience)}"] = audience,
        [$"{InternalTokenOptions.SectionName}:{nameof(InternalTokenOptions.JwksUrl)}"] = JwksUrl
    };

    /// <summary>Makes the service download this gateway's keys instead of calling <see cref="JwksUrl"/>.</summary>
    public void ServeKeys(IServiceCollection services)
        => services.AddHttpClient(InternalTokenAuthentication.KeysHttpClient)
            .ConfigurePrimaryHttpMessageHandler(() => new JwksHandler(_key));

    /// <summary>An internal token for <paramref name="userId"/>; the optional values make it one the service must reject.</summary>
    public string TokenFor(
        Guid userId,
        UserRole role = UserRole.User,
        string? issuer = null,
        string? tokenAudience = null,
        RSA? signingKey = null,
        TimeSpan? lifetime = null)
    {
        var key = signingKey ?? _key;
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(5));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Issuer,
            Audience = tokenAudience ?? audience,
            IssuedAt = expires.AddMinutes(-5),
            NotBefore = expires.AddMinutes(-5),
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [IdentityClaims.UserId] = userId.ToString(),
                [IdentityClaims.SessionId] = Guid.NewGuid().ToString(),
                [IdentityClaims.Role] = IdentityClaims.ValueOf(role)
            },
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(key) { KeyId = KeyId(key) }, SecurityAlgorithms.RsaSha256)
        });
    }

    private static string KeyId(RSA key) => Base64UrlEncoder.Encode(SHA256.HashData(key.ExportParameters(false).Modulus!));

    private sealed class JwksHandler(RSA key) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var publicKey = key.ExportParameters(false);
            var json = $$"""{"keys":[{"kty":"RSA","use":"sig","alg":"RS256","kid":"{{KeyId(key)}}","n":"{{Base64UrlEncoder.Encode(publicKey.Modulus)}}","e":"{{Base64UrlEncoder.Encode(publicKey.Exponent)}}"}]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
