using System.Net;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Vaultly.Gateway.UserTokens;
using Vaultly.Identity.PublicTypes;

using Yarp.ReverseProxy.Forwarder;

namespace Vaultly.Gateway.Tests;

/// <summary>
/// Hosts the real gateway in memory with three routes, Identity's JWKS stubbed and every service replaced by a
/// recording handler (<see cref="Destinations"/>).
/// </summary>
internal sealed class GatewayFactory : WebApplicationFactory<Program>
{
    public const string IdentityIssuer = "https://identity.test";
    public const string IdentityAudience = "vaultly.api";
    public const string InternalIssuer = "vaultly-gateway";

    public static readonly TestKey IdentityKey = new();
    public static readonly TestKey GatewayKey = new();

    public RecordingHandler Destinations { get; } = new(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") });

    public RecordingHandler IdentityKeys { get; } = new(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(IdentityKey.JwksJson()) });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("UserTokens:Issuer", IdentityIssuer);
        builder.UseSetting("UserTokens:Audience", IdentityAudience);
        builder.UseSetting("UserTokens:JwksUrl", $"{IdentityIssuer}/.well-known/jwks.json");
        builder.UseSetting("InternalTokens:Issuer", InternalIssuer);
        builder.UseSetting("InternalTokens:SigningKeyPem", GatewayKey.PrivateKeyPem);

        AddRoute(builder, "accounts", "/accounts/{**rest}", policy: null);
        AddRoute(builder, "admin", "/admin/{**rest}", GatewayPolicies.Admin);
        AddRoute(builder, "webhooks", "/webhooks/{**rest}", "anonymous");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IForwarderHttpClientFactory>();
            services.AddSingleton<IForwarderHttpClientFactory>(new StubForwarderClientFactory(Destinations));
            services.AddHttpClient(UserTokenAuthentication.KeysHttpClient).ConfigurePrimaryHttpMessageHandler(() => IdentityKeys);
        });
    }

    private static void AddRoute(IWebHostBuilder builder, string cluster, string path, string? policy)
    {
        builder.UseSetting($"ReverseProxy:Routes:{cluster}:ClusterId", cluster);
        builder.UseSetting($"ReverseProxy:Routes:{cluster}:Match:Path", path);
        if (policy is not null)
        {
            builder.UseSetting($"ReverseProxy:Routes:{cluster}:AuthorizationPolicy", policy);
        }

        builder.UseSetting($"ReverseProxy:Clusters:{cluster}:Destinations:main:Address", $"https://{cluster}.test");
    }

    private sealed class StubForwarderClientFactory(HttpMessageHandler handler) : IForwarderHttpClientFactory
    {
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) => new(handler, disposeHandler: false);
    }
}

/// <summary>
/// RSA key pair that signs test tokens and publishes itself as a JWKS.
/// </summary>
internal sealed class TestKey
{
    private readonly RSA _rsa = RSA.Create(2048);

    public TestKey()
    {
        Key = new RsaSecurityKey(_rsa) { KeyId = Guid.NewGuid().ToString("N") };
    }

    public RsaSecurityKey Key { get; }

    public string PrivateKeyPem => _rsa.ExportPkcs8PrivateKeyPem();

    public string JwksJson()
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = Key.KeyId });
        return $$"""{"keys":[{"kty":"RSA","use":"sig","alg":"RS256","kid":"{{jwk.Kid}}","n":"{{jwk.N}}","e":"{{jwk.E}}"}]}""";
    }
}

/// <summary>
/// Access tokens shaped like the Identity service's.
/// </summary>
internal static class UserTokens
{
    public const string UserId = "11111111-1111-1111-1111-111111111111";
    public const string SessionId = "22222222-2222-2222-2222-222222222222";

    public static string Create(
        UserRole role = UserRole.User,
        TestKey? key = null,
        string issuer = GatewayFactory.IdentityIssuer,
        string audience = GatewayFactory.IdentityAudience,
        TimeSpan? lifetime = null)
    {
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expires.AddMinutes(-15),
            NotBefore = expires.AddMinutes(-15),
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [IdentityClaims.UserId] = UserId,
                [IdentityClaims.SessionId] = SessionId,
                [IdentityClaims.Role] = IdentityClaims.ValueOf(role),
                [IdentityClaims.Email] = "user@vaultly.test",
                [IdentityClaims.Name] = "Vaultly User"
            },
            SigningCredentials = new SigningCredentials((key ?? GatewayFactory.IdentityKey).Key, SecurityAlgorithms.RsaSha256)
        });
    }
}

/// <summary>
/// HTTP handler stub that records requests and answers with a configurable response.
/// </summary>
internal sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = respond;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(Respond(request));
    }
}

internal static class HttpRequests
{
    public static HttpRequestMessage Get(string path, string? accessToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new("Bearer", accessToken);
        }

        return request;
    }
}
