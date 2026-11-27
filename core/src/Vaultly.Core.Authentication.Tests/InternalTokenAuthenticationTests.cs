using System.Net;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NUnit.Framework;

using Vaultly.Identity.PublicTypes;

namespace Vaultly.Core.Authentication.Tests;

[TestFixture]
public sealed class InternalTokenAuthenticationTests
{
    private const string GatewayIssuer = "vaultly-gateway";
    private const string ServiceAudience = "accounts";
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly RSA GatewayKey = RSA.Create(2048);

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InternalTokens:Issuer"] = GatewayIssuer,
            ["InternalTokens:Audience"] = ServiceAudience,
            ["InternalTokens:JwksUrl"] = "https://gateway.test/.well-known/jwks.json"
        });
        builder.Services.AddInternalTokenAuthentication(builder.Configuration);
        builder.Services.AddHttpClient(InternalTokenAuthentication.KeysHttpClient)
            .ConfigurePrimaryHttpMessageHandler(() => new JwksHandler(GatewayKey));

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/me", (HttpContext context) => context.User.GetUserId().ToString());
        _app.MapGet("/public", () => "ok").AllowAnonymous();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Test]
    public async Task Endpoint_WithGatewayTokenForThisService_SeesTheUser()
    {
        var response = await GetAsync("/me", Token());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(UserId.ToString()));
    }

    [Test]
    public async Task Endpoint_WithoutToken_IsUnauthorized()
    {
        var response = await GetAsync("/me", token: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static IEnumerable<TestCaseData> RejectedTokens()
    {
        yield return new TestCaseData(Token(audience: "transactions")).SetName("issued for another service");
        yield return new TestCaseData(Token(issuer: "https://identity.test")).SetName("not issued by the gateway");
        yield return new TestCaseData(Token(key: RSA.Create(2048))).SetName("signed by another key");
        yield return new TestCaseData(Token(lifetime: TimeSpan.FromMinutes(-5))).SetName("expired");
    }

    [TestCaseSource(nameof(RejectedTokens))]
    public async Task Endpoint_WithTokenNotMeantForThisService_IsUnauthorized(string token)
    {
        var response = await GetAsync("/me", token);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AnonymousEndpoint_WithoutToken_Answers()
    {
        var response = await GetAsync("/public", token: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private Task<HttpResponseMessage> GetAsync(string path, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new("Bearer", token);
        }

        return _client.SendAsync(request);
    }

    private static string Token(
        string issuer = GatewayIssuer,
        string audience = ServiceAudience,
        RSA? key = null,
        TimeSpan? lifetime = null)
    {
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(5));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expires.AddMinutes(-5),
            NotBefore = expires.AddMinutes(-5),
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [IdentityClaims.UserId] = UserId.ToString(),
                [IdentityClaims.SessionId] = Guid.NewGuid().ToString(),
                [IdentityClaims.Role] = IdentityClaims.ValueOf(UserRole.User)
            },
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(key ?? GatewayKey) { KeyId = KeyId(key ?? GatewayKey) }, SecurityAlgorithms.RsaSha256)
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
