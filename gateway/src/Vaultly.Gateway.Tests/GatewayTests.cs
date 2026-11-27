using System.Net;
using System.Text.Json;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NUnit.Framework;

using Vaultly.Identity.PublicTypes;

namespace Vaultly.Gateway.Tests;

[TestFixture]
public sealed class GatewayTests
{
    private GatewayFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new GatewayFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Request_WithoutToken_IsRejectedBeforeReachingTheService()
    {
        var response = await _client.SendAsync(HttpRequests.Get("/accounts/1"));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(response.Headers.WwwAuthenticate.ToString(), Is.EqualTo("Bearer"));
            Assert.That(_factory.Destinations.Requests, Is.Empty);
        });
    }

    private static IEnumerable<TestCaseData> InvalidUserTokens()
    {
        yield return new TestCaseData(UserTokens.Create(key: new TestKey())).SetName("signed by another key");
        yield return new TestCaseData(UserTokens.Create(issuer: "https://other.test")).SetName("another issuer");
        yield return new TestCaseData(UserTokens.Create(audience: "other.api")).SetName("another audience");
        yield return new TestCaseData(UserTokens.Create(lifetime: TimeSpan.FromMinutes(-5))).SetName("expired");
    }

    [TestCaseSource(nameof(InvalidUserTokens))]
    public async Task Request_WithInvalidToken_IsRejectedBeforeReachingTheService(string token)
    {
        var response = await _client.SendAsync(HttpRequests.Get("/accounts/1", token));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.WwwAuthenticate.ToString(), Is.EqualTo("Bearer error=\"invalid_token\""));
            Assert.That(_factory.Destinations.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task Request_WithValidToken_IsForwardedToTheRoutesServiceUnchanged()
    {
        var response = await _client.SendAsync(HttpRequests.Get("/accounts/1/transactions?from=2026-01-01", UserTokens.Create()));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_factory.Destinations.Requests.Single().RequestUri,
                Is.EqualTo(new Uri("https://accounts.test/accounts/1/transactions?from=2026-01-01")));
        });
    }

    [Test]
    public async Task ForwardedRequest_CarriesAnInternalTokenForThatServiceInsteadOfTheUserToken()
    {
        var userToken = UserTokens.Create(role: UserRole.Admin);

        await _client.SendAsync(HttpRequests.Get("/accounts/1", userToken));

        var authorization = _factory.Destinations.Requests.Single().Headers.Authorization;
        Assert.That(authorization?.Scheme, Is.EqualTo("Bearer"));
        Assert.That(authorization!.Parameter, Is.Not.EqualTo(userToken));

        var internalToken = await ValidateInternalTokenAsync(authorization.Parameter!, audience: "accounts");
        Assert.Multiple(() =>
        {
            Assert.That(internalToken.Subject, Is.EqualTo(UserTokens.UserId));
            Assert.That(internalToken.GetClaim(IdentityClaims.SessionId).Value, Is.EqualTo(UserTokens.SessionId));
            Assert.That(internalToken.GetClaim(IdentityClaims.Role).Value, Is.EqualTo("admin"));
            Assert.That(internalToken.TryGetClaim(IdentityClaims.Email, out _), Is.False);
        });
    }

    [Test]
    public async Task InternalToken_NeverOutlivesTheUserToken()
    {
        var userToken = UserTokens.Create(lifetime: TimeSpan.FromMinutes(2));

        await _client.SendAsync(HttpRequests.Get("/accounts/1", userToken));

        var internalToken = new JsonWebToken(_factory.Destinations.Requests.Single().Headers.Authorization!.Parameter);
        Assert.That(internalToken.ValidTo, Is.LessThanOrEqualTo(new JsonWebToken(userToken).ValidTo));
    }

    [Test]
    public async Task AdminRoute_ForUserRole_IsForbidden()
    {
        var response = await _client.SendAsync(HttpRequests.Get("/admin/users", UserTokens.Create(role: UserRole.User)));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(_factory.Destinations.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task AdminRoute_ForAdminRole_IsForwarded()
    {
        var response = await _client.SendAsync(HttpRequests.Get("/admin/users", UserTokens.Create(role: UserRole.Admin)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_factory.Destinations.Requests.Single().RequestUri, Is.EqualTo(new Uri("https://admin.test/admin/users")));
    }

    [Test]
    public async Task AnonymousRoute_WithoutToken_IsForwardedWithoutAuthorization()
    {
        var request = HttpRequests.Get("/webhooks/stripe");
        request.Headers.Authorization = new("Basic", "c3RyaXBlOnNlY3JldA==");

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_factory.Destinations.Requests.Single().Headers.Authorization, Is.Null);
    }

    [Test]
    public async Task UnreachableService_AnswersBadGatewayAsProblem()
    {
        _factory.Destinations.Respond = _ => throw new HttpRequestException("connection refused");

        var response = await _client.SendAsync(HttpRequests.Get("/accounts/1", UserTokens.Create()));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        });
    }

    [Test]
    public async Task Liveness_NeedsNoToken()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Readiness_WhenIdentityKeysLoad_IsHealthy()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Readiness_WhenIdentityKeysCannotLoad_IsUnavailable()
    {
        _factory.IdentityKeys.Respond = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        var response = await _client.GetAsync("/health/ready");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    private async Task<JsonWebToken> ValidateInternalTokenAsync(string token, string audience)
    {
        var jwks = new JsonWebKeySet(await _client.GetStringAsync("/.well-known/jwks.json"));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = GatewayFactory.InternalIssuer,
            ValidAudience = audience,
            IssuerSigningKeys = jwks.GetSigningKeys()
        });

        Assert.That(result.IsValid, Is.True, result.Exception?.Message);
        return (JsonWebToken)result.SecurityToken;
    }
}
