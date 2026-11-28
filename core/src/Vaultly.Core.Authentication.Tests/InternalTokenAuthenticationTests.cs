using System.Net;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Vaultly.Core.Testing;

namespace Vaultly.Core.Authentication.Tests;

[TestFixture]
public sealed class InternalTokenAuthenticationTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly TestGateway Gateway = new("accounts");

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(Gateway.Configuration);
        builder.Services.AddInternalTokenAuthentication(builder.Configuration);
        Gateway.ServeKeys(builder.Services);

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
        var response = await GetAsync("/me", Gateway.TokenFor(UserId));

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
        yield return new TestCaseData(Gateway.TokenFor(UserId, tokenAudience: "transactions")).SetName("issued for another service");
        yield return new TestCaseData(Gateway.TokenFor(UserId, issuer: "https://identity.test")).SetName("not issued by the gateway");
        yield return new TestCaseData(Gateway.TokenFor(UserId, signingKey: RSA.Create(2048))).SetName("signed by another key");
        yield return new TestCaseData(Gateway.TokenFor(UserId, lifetime: TimeSpan.FromMinutes(-5))).SetName("expired");
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
}
