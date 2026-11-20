using System.Net;
using System.Text;

using NUnit.Framework;

using Vaultly.Web.Api.Features.Auth.IdentityService;

namespace Vaultly.Web.Api.Tests.Features.Auth;

[TestFixture]
public sealed class IdentityServiceClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    private const string TokenJson = """
        {"accessToken":"access-1","expiresIn":900,"sessionId":"22222222-2222-2222-2222-222222222222",
         "user":{"id":"11111111-1111-1111-1111-111111111111","email":"user@vaultly.test","name":"Vaultly User"}}
        """;

    [Test]
    public async Task ExchangeCodeAsync_ReadsAccessTokenFromBodyAndRefreshTokenFromCookie()
    {
        var handler = new RecordingHandler(_ => TokenResponse("refresh%2Bvalue"));
        var sut = CreateSut(handler);

        var tokens = await sut.ExchangeCodeAsync("code-1", CancellationToken.None);
        var request = handler.Requests.Single();

        Assert.Multiple(async () =>
        {
            Assert.That(request.RequestUri!.ToString(), Is.EqualTo("https://identity.test/token"));
            Assert.That(await request.Content!.ReadAsStringAsync(), Is.EqualTo("{\"code\":\"code-1\"}"));
            Assert.That(tokens.AccessToken, Is.EqualTo("access-1"));
            Assert.That(tokens.AccessTokenExpiresAt, Is.EqualTo(Now.AddSeconds(900)));
            Assert.That(tokens.RefreshToken, Is.EqualTo("refresh+value"));
            Assert.That(tokens.RefreshTokenExpiresAt, Is.EqualTo(new DateTimeOffset(2026, 12, 21, 12, 0, 0, TimeSpan.Zero)));
            Assert.That(tokens.User.Email, Is.EqualTo("user@vaultly.test"));
        });
    }

    [Test]
    public void ExchangeCodeAsync_WhenRefreshCookieMissing_Throws()
    {
        var sut = CreateSut(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json"),
        }));

        Assert.ThrowsAsync<IdentityServiceException>(() => sut.ExchangeCodeAsync("code-1", CancellationToken.None));
    }

    [Test]
    public async Task RefreshAsync_SendsRefreshTokenAsCookie()
    {
        var handler = new RecordingHandler(_ => TokenResponse("refresh-2"));
        var sut = CreateSut(handler);

        var tokens = await sut.RefreshAsync("refresh+1", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Single().Headers.GetValues("Cookie").Single(), Is.EqualTo("vaultly_refresh=refresh%2B1"));
            Assert.That(tokens!.RefreshToken, Is.EqualTo("refresh-2"));
        });
    }

    [Test]
    public async Task RefreshAsync_WhenRejected_ReturnsNull()
    {
        var sut = CreateSut(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        Assert.That(await sut.RefreshAsync("refresh-1", CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task RevokeSessionAsync_WhenSessionMissing_ReturnsFalse()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateSut(handler);

        var revoked = await sut.RevokeSessionAsync("access-1", Guid.Empty, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(revoked, Is.False);
            Assert.That(handler.Requests.Single().Headers.Authorization!.ToString(), Is.EqualTo("Bearer access-1"));
        });
    }

    private static IdentityServiceClient CreateSut(HttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("https://identity.test/") }, new FixedTimeProvider(Now));

    private static HttpResponseMessage TokenResponse(string encodedRefreshToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json"),
        };
        response.Headers.Add(
            "Set-Cookie",
            $"vaultly_refresh={encodedRefreshToken}; expires=Mon, 21 Dec 2026 12:00:00 GMT; path=/; secure; samesite=none; httponly");
        return response;
    }
}
