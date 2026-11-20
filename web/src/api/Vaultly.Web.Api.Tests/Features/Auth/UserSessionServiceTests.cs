using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Vaultly.Web.Api.Features.Auth;
using Vaultly.Web.Api.Features.Auth.IdentityService;
using Vaultly.Web.Api.Features.Auth.Sessions;

namespace Vaultly.Web.Api.Tests.Features.Auth;

[TestFixture]
public sealed class UserSessionServiceTests
{
    private const string SessionId = "session-under-test";

    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    private FixedTimeProvider _time = null!;
    private Mock<IIdentityServiceClient> _identity = null!;
    private IWebSessionStore _store = null!;
    private SessionRefreshLock _refreshLock = null!;
    private IOptions<AuthOptions> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FixedTimeProvider(Now);
        _identity = new Mock<IIdentityServiceClient>(MockBehavior.Strict);
        _store = new DistributedWebSessionStore(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new EphemeralDataProtectionProvider(),
            NullLogger<DistributedWebSessionStore>.Instance);
        _refreshLock = new SessionRefreshLock();
        _options = Options.Create(new AuthOptions
        {
            IdentityBaseUrl = "https://identity.test",
            PublicOrigin = "https://app.test",
            SessionCookieName = "session",
            AccessTokenRefreshSkewSeconds = 60,
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenIsValid_ReturnsItWithoutRefreshing()
    {
        await _store.SetAsync(SessionId, WebSession.Create(TestData.Tokens(Now), Now), CancellationToken.None);

        var token = await CreateSut().GetAccessTokenAsync(CreateContext(), CancellationToken.None);

        Assert.That(token, Is.EqualTo("access-1"));
        _identity.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenExpiresSoon_RefreshesAndStoresRotatedTokens()
    {
        await _store.SetAsync(SessionId, WebSession.Create(TestData.Tokens(Now, accessLifetime: TimeSpan.FromSeconds(30)), Now), CancellationToken.None);
        _identity.Setup(x => x.RefreshAsync("refresh-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Tokens(Now, "access-2", "refresh-2"));

        var token = await CreateSut().GetAccessTokenAsync(CreateContext(), CancellationToken.None);
        var stored = await _store.GetAsync(SessionId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.EqualTo("access-2"));
            Assert.That(stored!.RefreshToken, Is.EqualTo("refresh-2"));
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenRefreshIsRejected_RemovesSessionAndClearsCookie()
    {
        await _store.SetAsync(SessionId, WebSession.Create(TestData.Tokens(Now, accessLifetime: TimeSpan.Zero), Now), CancellationToken.None);
        _identity.Setup(x => x.RefreshAsync("refresh-1", It.IsAny<CancellationToken>())).ReturnsAsync((IdentityTokenSet?)null);
        var context = CreateContext();

        var token = await CreateSut().GetAccessTokenAsync(context, CancellationToken.None);

        Assert.Multiple(async () =>
        {
            Assert.That(token, Is.Null);
            Assert.That(await _store.GetAsync(SessionId, CancellationToken.None), Is.Null);
            Assert.That(context.Response.Headers.SetCookie.ToString(), Does.StartWith("session=;"));
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenCalledConcurrently_RefreshesOnlyOnce()
    {
        await _store.SetAsync(SessionId, WebSession.Create(TestData.Tokens(Now, accessLifetime: TimeSpan.Zero), Now), CancellationToken.None);
        _identity.Setup(x => x.RefreshAsync("refresh-1", It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(50);
                return TestData.Tokens(Now, "access-2", "refresh-2");
            });

        var tokens = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => CreateSut().GetAccessTokenAsync(CreateContext(), CancellationToken.None)));

        Assert.That(tokens, Is.All.EqualTo("access-2"));
        _identity.Verify(x => x.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SignInAsync_WhenPreviousSessionExists_ReplacesItWithNewIdentifier()
    {
        await _store.SetAsync(SessionId, WebSession.Create(TestData.Tokens(Now), Now), CancellationToken.None);
        var context = CreateContext();

        await CreateSut().SignInAsync(context, TestData.Tokens(Now, "access-new"), CancellationToken.None);

        Assert.Multiple(async () =>
        {
            Assert.That(await _store.GetAsync(SessionId, CancellationToken.None), Is.Null, "session fixation: old id must die");
            Assert.That(context.Response.Headers.SetCookie.ToString(), Does.StartWith("session=").And.Not.Contain(SessionId));
        });
    }

    private UserSessionService CreateSut()
        => new(_store, new SessionCookie(_options), _identity.Object, _refreshLock, _time, _options, NullLogger<UserSessionService>.Instance);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"session={SessionId}";
        return context;
    }
}
