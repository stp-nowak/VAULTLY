using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Serilog;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.UseCases.BuildAuthorizeUrl;
using Vaultly.Identity.Application.UseCases.ExchangeAuthCode;
using Vaultly.Identity.Application.UseCases.GetEnabledProviders;
using Vaultly.Identity.Application.UseCases.HandleOAuthCallback;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class IdentityUseCaseHandlerTests
{
    [Test]
    public async Task GetEnabledProvidersHandler_WhenSomeDisabled_ReturnsOnlyEnabled()
    {
        var handler = new GetEnabledProvidersHandler(
            [
                new TestProvider("google", "Google", true),
                new TestProvider("github", "GitHub", false)
            ]);

        var result = await handler.Handle(new GetEnabledProvidersQuery(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(new[] { new ProviderInfo("google", "Google", "/google/authorize") }));
    }

    [Test]
    public async Task BuildAuthorizeUrlHandler_WhenProviderEnabled_PersistsOAuthStateAndReturnsAuthorizeUrl()
    {
        var oauthRepo = new Mock<IOAuthStateRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var tokenGenerator = new Mock<ITokenGenerator>();
        var clock = CreateClock();
        OAuthState? capturedState = null;

        tokenGenerator.SetupSequence(x => x.GenerateToken(It.IsAny<int>()))
            .Returns("generated-state")
            .Returns("generated-verifier")
            .Returns("generated-nonce");
        oauthRepo.Setup(x => x.Add(It.IsAny<OAuthState>()))
            .Callback<OAuthState>(state => capturedState = state);

        var handler = new BuildAuthorizeUrlHandler(
            oauthRepo.Object,
            unitOfWork.Object,
            new[] { new TestProvider("google", "Google", true) },
            tokenGenerator.Object,
            clock.Object,
            CreateIdentityOptions(),
            CreateLogger());

        var url = await handler.Handle(
            new BuildAuthorizeUrlQuery("google", "https://app.test/callback"),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(capturedState, Is.Not.Null);
            Assert.That(capturedState!.RedirectUri!.Value, Is.EqualTo("https://app.test/callback"));
            Assert.That(url, Does.Contain("state=generated-state"));
            Assert.That(url, Does.Contain("nonce=generated-nonce"));
        });
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void BuildAuthorizeUrlHandler_WhenRedirectUriNotAllowed_ThrowsInvalidValueException()
    {
        var handler = new BuildAuthorizeUrlHandler(
            new Mock<IOAuthStateRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            [new TestProvider("google", "Google", true)],
            new Mock<ITokenGenerator>().Object,
            CreateClock().Object,
            CreateIdentityOptions(),
            CreateLogger());

        Assert.That(
            async () => await handler.Handle(
                new BuildAuthorizeUrlQuery("google", "https://evil.test/callback"),
                CancellationToken.None),
            Throws.TypeOf<InvalidValueException>());
    }

    [Test]
    public async Task BuildAuthorizeUrlHandler_WhenRedirectUriMatchesWildcardPattern_PersistsOAuthStateAndReturnsAuthorizeUrl()
    {
        var oauthRepo = new Mock<IOAuthStateRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var tokenGenerator = new Mock<ITokenGenerator>();
        var clock = CreateClock();
        OAuthState? capturedState = null;

        tokenGenerator.SetupSequence(x => x.GenerateToken(It.IsAny<int>()))
            .Returns("generated-state")
            .Returns("generated-verifier")
            .Returns("generated-nonce");
        oauthRepo.Setup(x => x.Add(It.IsAny<OAuthState>()))
            .Callback<OAuthState>(state => capturedState = state);

        var handler = new BuildAuthorizeUrlHandler(
            oauthRepo.Object,
            unitOfWork.Object,
            [new TestProvider("google", "Google", true)],
            tokenGenerator.Object,
            clock.Object,
            CreateIdentityOptions(allowedRedirectUris: ["http://localhost:4200/*"]),
            CreateLogger());

        var url = await handler.Handle(
            new BuildAuthorizeUrlQuery("google", "http://localhost:4200/oauth/callback?returnUrl=%2Fbudget"),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(capturedState, Is.Not.Null);
            Assert.That(capturedState!.RedirectUri!.Value, Is.EqualTo("http://localhost:4200/oauth/callback?returnUrl=%2Fbudget"));
            Assert.That(url, Does.Contain("state=generated-state"));
        });
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void BuildAuthorizeUrlHandler_WhenRedirectUriFallsOutsideWildcardPattern_ThrowsInvalidValueException()
    {
        var handler = new BuildAuthorizeUrlHandler(
            new Mock<IOAuthStateRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            [new TestProvider("google", "Google", true)],
            new Mock<ITokenGenerator>().Object,
            CreateClock().Object,
            CreateIdentityOptions(allowedRedirectUris: ["http://localhost:4200/oauth/*"]),
            CreateLogger());

        Assert.That(
            async () => await handler.Handle(
                new BuildAuthorizeUrlQuery("google", "http://localhost:4200/other/callback"),
                CancellationToken.None),
            Throws.TypeOf<InvalidValueException>());
    }

    [Test]
    public void BuildAuthorizeUrlHandler_WhenWildcardPatternConfigurationIsInvalid_ThrowsInvalidOperationException()
    {
        var handler = new BuildAuthorizeUrlHandler(
            new Mock<IOAuthStateRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            [new TestProvider("google", "Google", true)],
            new Mock<ITokenGenerator>().Object,
            CreateClock().Object,
            CreateIdentityOptions(allowedRedirectUris: ["http://localhost:4200/*/callback"]),
            CreateLogger());

        Assert.That(
            async () => await handler.Handle(
                new BuildAuthorizeUrlQuery("google", "http://localhost:4200/oauth/callback"),
                CancellationToken.None),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task ExchangeAuthCodeHandler_WhenCodeValid_ReturnsTokensAndPersistsChanges()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var session = Session.Create(
            UserId.New(),
            new AuthMethod("google"),
            UserAgent.FromNullable("unit-test"),
            IpAddress.FromNullable("127.0.0.1"),
            now);
        session.IssueAuthCode(new CodeHash("hashed-auth-code"), now.AddMinutes(5));

        var user = User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), true, now);
        var sessionRepository = new Mock<ISessionRepository>();
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var tokenGenerator = new Mock<ITokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var clock = CreateClock(now);

        tokenHasher.Setup(x => x.HashToken("plain-auth-code")).Returns("hashed-auth-code");
        tokenHasher.Setup(x => x.HashToken("plain-refresh-token")).Returns("hashed-refresh-token");
        tokenGenerator.Setup(x => x.GenerateToken(64)).Returns("plain-refresh-token");
        jwtTokenService.Setup(x => x.CreateAccessToken(user, session, now)).Returns("access-token");
        sessionRepository.Setup(x => x.GetByAuthCodeHashAsync(new CodeHash("hashed-auth-code"), CancellationToken.None)).ReturnsAsync(session);
        userRepository.Setup(x => x.GetByIdAsync(session.UserId, CancellationToken.None)).ReturnsAsync(user);

        var handler = new ExchangeAuthCodeHandler(
            sessionRepository.Object,
            userRepository.Object,
            unitOfWork.Object,
            jwtTokenService.Object,
            tokenGenerator.Object,
            tokenHasher.Object,
            clock.Object,
            CreateIdentityOptions(cookieDomain: ".vaultly.local"),
            CreateLogger());

        var result = await handler.Handle(new ExchangeAuthCodeCommand("plain-auth-code"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.AccessToken, Is.EqualTo("access-token"));
            Assert.That(result.RefreshToken, Is.EqualTo("plain-refresh-token"));
            Assert.That(result.SessionId, Is.EqualTo(session.Id.Value));
            Assert.That(result.CookieDomain, Is.EqualTo(".vaultly.local"));
            Assert.That(result.User, Is.EqualTo(new AuthenticatedUserDto(user.Id.Value, user.Email.Value, user.Name.Value)));
        });
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task HandleOAuthCallbackHandler_WhenUserDoesNotExist_CreatesUserSessionAndRedirectUrl()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var oauthState = OAuthState.Create(
            new OAuthStateValue("state-value"),
            new CodeVerifier("stored-code-verifier"),
            new Nonce("expected-nonce"),
            now.AddMinutes(5),
            new RedirectUri("https://app.test/callback"));

        var oauthRepo = new Mock<IOAuthStateRepository>();
        var userRepo = new Mock<IUserRepository>();
        var sessionRepo = new Mock<ISessionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var tokenGenerator = new Mock<ITokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        var clock = CreateClock(now);

        User? capturedUser = null;
        Session? capturedSession = null;

        oauthRepo.Setup(x => x.GetByStateAsync(new OAuthStateValue("state-value"), CancellationToken.None)).ReturnsAsync(oauthState);
        tokenGenerator.Setup(x => x.GenerateToken(32)).Returns("plain-auth-code");
        tokenHasher.Setup(x => x.HashToken("plain-auth-code")).Returns("hashed-auth-code");
        userRepo.Setup(x => x.GetByExternalIdentityAsync(new ProviderId("google"), new ProviderUserId("subject-1"), CancellationToken.None))
            .ReturnsAsync((User?)null);
        userRepo.Setup(x => x.GetByEmailAsync(new EmailAddress("user@example.com"), CancellationToken.None))
            .ReturnsAsync((User?)null);
        userRepo.Setup(x => x.Add(It.IsAny<User>())).Callback<User>(user => capturedUser = user);
        sessionRepo.Setup(x => x.Add(It.IsAny<Session>())).Callback<Session>(session => capturedSession = session);

        var handler = new HandleOAuthCallbackHandler(
            oauthRepo.Object,
            userRepo.Object,
            sessionRepo.Object,
            unitOfWork.Object,
            new[]
            {
                new TestProvider(
                    "google",
                    "Google",
                    true,
                    new ExternalIdentityInfo("subject-1", "user@example.com", true, "Test User", "expected-nonce"))
            },
            tokenGenerator.Object,
            tokenHasher.Object,
            clock.Object,
            CreateIdentityOptions(),
            CreateLogger());

        var result = await handler.Handle(
            new HandleOAuthCallbackCommand("google", "provider-code", "state-value", "unit-test", "127.0.0.1"),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(capturedUser, Is.Not.Null);
            Assert.That(capturedSession, Is.Not.Null);
            Assert.That(result.RedirectUrl, Does.StartWith("https://app.test/callback?"));
            Assert.That(result.RedirectUrl, Does.Contain("code=plain-auth-code"));
            Assert.That(result.RedirectUrl, Does.Not.Contain("returnUrl="));
        });
        oauthRepo.Verify(x => x.Remove(oauthState), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    private static Mock<IClock> CreateClock(DateTimeOffset? now = null)
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now ?? new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        return clock;
    }

    private static IOptions<IdentityOptions> CreateIdentityOptions(string? cookieDomain = null, params IEnumerable<string> allowedRedirectUris)
    {
        var configuredRedirectUris = allowedRedirectUris.ToArray();

        return Microsoft.Extensions.Options.Options.Create(new IdentityOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            PublicBaseUrl = "https://identity.test",
            JwtPrivateKeyPem = "pem",
            AccessTokenTtlMinutes = 15,
            RefreshTokenTtlDays = 14,
            AuthCodeTtlMinutes = 5,
            OAuthStateTtlMinutes = 10,
            RefreshTokenCookieDomain = cookieDomain,
            AllowedRedirectUris = configuredRedirectUris.Length > 0 ? [.. configuredRedirectUris] : ["https://app.test/callback"]
        });
    }

    private static ILogger CreateLogger() => new LoggerConfiguration().CreateLogger();

    private sealed class TestProvider(
        string id,
        string name,
        bool enabled,
        ExternalIdentityInfo? exchangeResult = null) : IExternalOAuthProvider
    {
        public ProviderId Id { get; } = new(id);
        public string Name { get; } = name;
        public bool IsEnabled { get; } = enabled;
        public string AuthorizationEndpoint { get; } = "https://provider.test/auth";
        public string ClientId { get; } = "client";
        public string RedirectUri { get; } = "https://identity.test/callback";
        public IReadOnlyList<string> Scopes { get; } = ["openid", "email", "profile"];

        public Task<ExternalIdentityInfo> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
        {
            if (exchangeResult is null)
            {
                throw new NotSupportedException("ExchangeCodeAsync is not used in this test.");
            }

            return Task.FromResult(exchangeResult);
        }
    }
}