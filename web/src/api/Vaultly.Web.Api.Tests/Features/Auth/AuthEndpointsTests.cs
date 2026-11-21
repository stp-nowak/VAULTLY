using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.WebUtilities;

using Moq;

using NUnit.Framework;

using Vaultly.Web.Api.Features.Auth;

namespace Vaultly.Web.Api.Tests.Features.Auth;

[TestFixture]
public sealed class AuthEndpointsTests
{
    private const string CallbackUrl = "https://app.vaultly.local/api/auth/callback";

    private WebApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApiFactory();
        _client = _factory.CreateRawClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Login_RedirectsToAuthorizeWithPkceAndSetsEncryptedLoginCookie()
    {
        var response = await _client.GetAsync("/api/auth/login?returnUrl=/app/budgets");

        var location = response.Headers.Location!;
        var query = QueryHelpers.ParseQuery(location.Query);
        var loginCookie = TestData.SetCookies(response).Single(x => x.Name == "__Host-vaultly-login");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            Assert.That(location.GetLeftPart(UriPartial.Path), Is.EqualTo("https://identity.vaultly.local/authorize"));
            Assert.That(query["response_type"].ToString(), Is.EqualTo("code"));
            Assert.That(query["client_id"].ToString(), Is.EqualTo("vaultly-web"));
            Assert.That(query["redirect_uri"].ToString(), Is.EqualTo(CallbackUrl));
            Assert.That(query["code_challenge_method"].ToString(), Is.EqualTo("S256"));
            Assert.That(query["code_challenge"].ToString(), Has.Length.EqualTo(43));
            Assert.That(query["state"].ToString(), Has.Length.GreaterThanOrEqualTo(40));
            Assert.That(loginCookie.HttpOnly, Is.True);
            Assert.That(loginCookie.Value.ToString(), Does.Not.Contain(query["state"].ToString()), "login state must be encrypted");
        });
    }

    [Test]
    public async Task Callback_ExchangesCodeWithVerifierMatchingTheChallenge()
    {
        string? verifier = null;
        _factory.Identity
            .Setup(x => x.ExchangeCodeAsync("code-1", It.IsAny<string>(), CallbackUrl, It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, codeVerifier, _, _) => verifier = codeVerifier)
            .ReturnsAsync(TestData.Tokens(DateTimeOffset.UtcNow));

        var login = await _client.GetAsync("/api/auth/login");
        var query = QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        await SendAsync(
            HttpMethod.Get,
            $"/api/auth/callback?code=code-1&state={Uri.EscapeDataString(query["state"].ToString())}",
            TestData.CookieHeader(login),
            csrf: false);

        Assert.That(AuthEndpoints.CreateCodeChallenge(verifier!), Is.EqualTo(query["code_challenge"].ToString()));
    }

    [Test]
    public async Task Callback_WithValidState_CreatesSessionAndRedirectsToReturnUrl()
    {
        SetupCodeExchange();

        var (callback, _) = await LoginAndCallbackAsync("/app/budgets");
        var sessionCookie = TestData.SetCookies(callback).Single(x => x.Name == "__Host-vaultly-session");

        Assert.Multiple(() =>
        {
            Assert.That(callback.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            Assert.That(callback.Headers.Location!.OriginalString, Is.EqualTo("/app/budgets"));
            Assert.That(sessionCookie.HttpOnly, Is.True);
            Assert.That(sessionCookie.Secure, Is.True);
            Assert.That(sessionCookie.SameSite, Is.EqualTo(Microsoft.Net.Http.Headers.SameSiteMode.Strict));
            Assert.That(sessionCookie.Value.ToString(), Does.Not.Contain("access-1").And.Not.Contain("refresh-1"));
        });
    }

    [TestCase("//evil.example")]
    [TestCase("/\\evil.example")]
    [TestCase("https://evil.example/app")]
    public async Task Callback_WithNonLocalReturnUrl_RedirectsToDefaultPath(string returnUrl)
    {
        SetupCodeExchange();

        var (callback, _) = await LoginAndCallbackAsync(returnUrl);

        Assert.That(callback.Headers.Location!.OriginalString, Is.EqualTo("/app"));
    }

    [Test]
    public async Task Callback_WithMismatchedState_RedirectsToLoginWithoutExchangingCode()
    {
        var login = await _client.GetAsync("/api/auth/login");

        var callback = await SendAsync(HttpMethod.Get, "/api/auth/callback?code=code-1&state=forged", TestData.CookieHeader(login), csrf: false);

        Assert.Multiple(() =>
        {
            Assert.That(callback.Headers.Location!.OriginalString, Is.EqualTo("/login?error=invalid_state"));
            Assert.That(TestData.SetCookies(callback).Any(x => x.Name == "__Host-vaultly-session" && x.Value.Length > 0), Is.False);
        });
        _factory.Identity.Verify(
            x => x.ExchangeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task User_WithSession_ReturnsUserWithoutTokens()
    {
        SetupCodeExchange();
        var (_, sessionCookie) = await LoginAndCallbackAsync("/app");

        var response = await SendAsync(HttpMethod.Get, "/api/auth/user", sessionCookie);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.GetProperty("user").GetProperty("email").GetString(), Is.EqualTo(TestData.User.Email));
            Assert.That(body, Does.Not.Contain("access-1").And.Not.Contain("refresh-1"));
        });
    }

    [Test]
    public async Task User_WithoutSession_Returns401()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/auth/user", cookieHeader: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task User_WithoutCsrfHeader_Returns400()
    {
        SetupCodeExchange();
        var (_, sessionCookie) = await LoginAndCallbackAsync("/app");

        var response = await SendAsync(HttpMethod.Get, "/api/auth/user", sessionCookie, csrf: false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Logout_RevokesTokensInvalidatesCookieAndReturnsSsoLogoutUrl()
    {
        SetupCodeExchange();
        _factory.Identity.Setup(x => x.RevokeAsync("refresh-1", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var (_, sessionCookie) = await LoginAndCallbackAsync("/app");

        var logout = await SendAsync(HttpMethod.Post, "/api/auth/logout", sessionCookie);
        var logoutUrl = new Uri(JsonDocument.Parse(await logout.Content.ReadAsStringAsync()).RootElement.GetProperty("logoutUrl").GetString()!);
        var user = await SendAsync(HttpMethod.Get, "/api/auth/user", sessionCookie);

        Assert.Multiple(() =>
        {
            Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(logoutUrl.GetLeftPart(UriPartial.Path), Is.EqualTo("https://identity.vaultly.local/logout"));
            Assert.That(QueryHelpers.ParseQuery(logoutUrl.Query)["post_logout_redirect_uri"].ToString(), Is.EqualTo("https://app.vaultly.local/"));
            Assert.That(user.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "old cookie must not resurrect the session");
        });
        _factory.Identity.Verify(x => x.RevokeAsync("refresh-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ApiProxy_WithSession_ForwardsWithBearerTokenAndWithoutCookies()
    {
        SetupCodeExchange();
        var (_, sessionCookie) = await LoginAndCallbackAsync("/app");

        var response = await SendAsync(HttpMethod.Get, "/api/budgets/42?month=2026-11", sessionCookie);
        var forwarded = _factory.Backend.Requests.Single();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(forwarded.RequestUri!.ToString(), Is.EqualTo("https://backend.test/budgets/42?month=2026-11"));
            Assert.That(forwarded.Headers.Authorization?.ToString(), Is.EqualTo("Bearer access-1"));
            Assert.That(forwarded.Headers.Contains("Cookie"), Is.False);
        });
    }

    [Test]
    public async Task ApiProxy_WithoutSession_Returns401AndDoesNotForward()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/budgets", cookieHeader: null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(_factory.Backend.Requests, Is.Empty);
        });
    }

    [TestCase(null, "/app")]
    [TestCase("/app/goals?tab=1", "/app/goals?tab=1")]
    [TestCase("//evil.example", "/app")]
    [TestCase("/api/auth/logout", "/app")]
    public void SanitizeReturnUrl_AllowsOnlyLocalSpaPaths(string? returnUrl, string expected)
        => Assert.That(AuthEndpoints.SanitizeReturnUrl(returnUrl, "/app"), Is.EqualTo(expected));

    /// <summary>
    /// Configures a successful code exchange for <c>code-1</c>.
    /// </summary>
    private void SetupCodeExchange()
        => _factory.Identity
            .Setup(x => x.ExchangeCodeAsync("code-1", It.IsAny<string>(), CallbackUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Tokens(DateTimeOffset.UtcNow));

    /// <summary>
    /// Runs login + callback like a browser and returns the callback response and the resulting session cookie header.
    /// </summary>
    private async Task<(HttpResponseMessage Callback, string SessionCookie)> LoginAndCallbackAsync(string returnUrl)
    {
        var login = await _client.GetAsync($"/api/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var state = QueryHelpers.ParseQuery(login.Headers.Location!.Query)["state"].ToString();

        var callback = await SendAsync(
            HttpMethod.Get,
            $"/api/auth/callback?code=code-1&state={Uri.EscapeDataString(state)}",
            TestData.CookieHeader(login),
            csrf: false);

        return (callback, TestData.CookieHeader(callback));
    }

    /// <summary>
    /// Sends a request the way the SPA does (same-origin cookie + CSRF header).
    /// </summary>
    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? cookieHeader, bool csrf = true)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        if (csrf)
        {
            request.Headers.Add("X-CSRF", "1");
        }

        return _client.SendAsync(request);
    }
}
