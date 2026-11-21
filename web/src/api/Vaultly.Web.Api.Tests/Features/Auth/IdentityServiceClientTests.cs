using System.Net;
using System.Text;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using NUnit.Framework;

using Vaultly.Web.Api.Features.Auth;
using Vaultly.Web.Api.Features.Auth.IdentityService;

namespace Vaultly.Web.Api.Tests.Features.Auth;

[TestFixture]
public sealed class IdentityServiceClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    private const string TokenJson = """
        {"access_token":"access-1","token_type":"Bearer","expires_in":900,"refresh_token":"refresh-1","refresh_expires_in":2592000,
         "session_id":"22222222-2222-2222-2222-222222222222",
         "user":{"id":"11111111-1111-1111-1111-111111111111","email":"user@vaultly.test","name":"Vaultly User"}}
        """;

    [Test]
    public async Task ExchangeCodeAsync_PostsAuthorizationCodeGrantWithClientCredentialsAndPkce()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, TokenJson));

        var tokens = await CreateSut(handler).ExchangeCodeAsync("code-1", "verifier-1", "https://app.test/api/auth/callback", CancellationToken.None);
        var request = handler.Requests.Single();
        var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(request.RequestUri!.ToString(), Is.EqualTo("https://identity.test/token"));
            Assert.That(form["grant_type"].ToString(), Is.EqualTo("authorization_code"));
            Assert.That(form["code"].ToString(), Is.EqualTo("code-1"));
            Assert.That(form["code_verifier"].ToString(), Is.EqualTo("verifier-1"));
            Assert.That(form["redirect_uri"].ToString(), Is.EqualTo("https://app.test/api/auth/callback"));
            Assert.That(form["client_id"].ToString(), Is.EqualTo("vaultly-web"));
            Assert.That(form["client_secret"].ToString(), Is.EqualTo("secret"));
            Assert.That(tokens.AccessTokenExpiresAt, Is.EqualTo(Now.AddSeconds(900)));
            Assert.That(tokens.RefreshToken, Is.EqualTo("refresh-1"));
            Assert.That(tokens.RefreshTokenExpiresAt, Is.EqualTo(Now.AddDays(30)));
            Assert.That(tokens.User.Email, Is.EqualTo("user@vaultly.test"));
        });
    }

    [Test]
    public async Task RefreshAsync_WhenGrantRejected_ReturnsNull()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}"""));

        var tokens = await CreateSut(handler).RefreshAsync("refresh-1", CancellationToken.None);
        var form = QueryHelpers.ParseQuery(await handler.Requests.Single().Content!.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(tokens, Is.Null);
            Assert.That(form["grant_type"].ToString(), Is.EqualTo("refresh_token"));
            Assert.That(form["refresh_token"].ToString(), Is.EqualTo("refresh-1"));
        });
    }

    [Test]
    public void RefreshAsync_WhenClientRejected_Throws()
    {
        var sut = CreateSut(new RecordingHandler(_ => Json(HttpStatusCode.Unauthorized, """{"error":"invalid_client"}""")));

        Assert.ThrowsAsync<IdentityServiceException>(() => sut.RefreshAsync("refresh-1", CancellationToken.None));
    }

    [Test]
    public async Task RevokeAsync_PostsTokenToRevocationEndpoint()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await CreateSut(handler).RevokeAsync("refresh-1", CancellationToken.None);
        var request = handler.Requests.Single();

        Assert.Multiple(async () =>
        {
            Assert.That(request.RequestUri!.ToString(), Is.EqualTo("https://identity.test/revoke"));
            Assert.That(QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync())["token"].ToString(), Is.EqualTo("refresh-1"));
        });
    }

    [Test]
    public async Task RevokeSessionAsync_WhenSessionMissing_ReturnsFalse()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var revoked = await CreateSut(handler).RevokeSessionAsync("access-1", Guid.Empty, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(revoked, Is.False);
            Assert.That(handler.Requests.Single().Headers.Authorization!.ToString(), Is.EqualTo("Bearer access-1"));
        });
    }

    private static IdentityServiceClient CreateSut(HttpMessageHandler handler)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("https://identity.test/") },
            Options.Create(new AuthOptions { ClientId = "vaultly-web", ClientSecret = "secret" }),
            new FixedTimeProvider(Now));

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
