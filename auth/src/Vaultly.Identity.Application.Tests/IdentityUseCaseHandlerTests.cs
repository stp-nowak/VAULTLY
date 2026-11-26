using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Serilog;

using Vaultly.Identity.Application.Commands.EndSession;
using Vaultly.Identity.Application.Commands.ExchangeAuthCode;
using Vaultly.Identity.Application.Commands.HandleOAuthCallback;
using Vaultly.Identity.Application.Commands.RefreshSession;
using Vaultly.Identity.Application.Commands.StartAuthorization;
using Vaultly.Identity.Application.Commands.StartExternalLogin;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Queries.GetAuthorizationRequest;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class IdentityUseCaseHandlerTests
{
    // RFC 7636 Appendix B test vector.
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
    private const string ClientIdValue = "vaultly-web";
    private const string ClientSecret = "test-secret";
    private const string Callback = "https://app.test/api/auth/callback";

    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private Mock<IAuthorizationRequestRepository> _requests = null!;
    private Mock<ISessionRepository> _sessions = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITokenGenerator> _tokenGenerator = null!;
    private Mock<ITokenHasher> _tokenHasher = null!;
    private IOptions<IdentityOptions> _options = null!;
    private ClientRegistry _clients = null!;

    [SetUp]
    public void SetUp()
    {
        _requests = new Mock<IAuthorizationRequestRepository>();
        _sessions = new Mock<ISessionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tokenGenerator = new Mock<ITokenGenerator>();
        _tokenHasher = new Mock<ITokenHasher>();
        _tokenHasher.Setup(x => x.HashToken(It.IsAny<string>())).Returns<string>(value => $"hash:{value}");
        _options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            PublicBaseUrl = "https://identity.test",
            AccessTokenTtlMinutes = 15,
            RefreshTokenTtlDays = 14,
            AuthCodeTtlMinutes = 2,
            OAuthStateTtlMinutes = 10,
            AuthorizationRequestTtlMinutes = 10,
            SsoSessionTtlHours = 12,
            Clients =
            [
                new OAuthClientOptions
                {
                    ClientId = ClientIdValue,
                    ClientSecret = ClientSecret,
                    DisplayName = "Vaultly",
                    RedirectUris = [Callback],
                    PostLogoutRedirectUris = ["https://app.test/"],
                },
            ],
        });
        _clients = new ClientRegistry(_options);
    }

    [Test]
    public void StartAuthorization_WhenClientUnknown_ThrowsWithoutRedirect()
    {
        var ex = Assert.ThrowsAsync<OAuthProtocolException>(() => CreateStartAuthorizationHandler().Handle(
            Authorize(clientId: "unknown"),
            CancellationToken.None));

        Assert.That(ex!.Error, Is.EqualTo(OAuthError.InvalidRequest));
    }

    [Test]
    public void StartAuthorization_WhenRedirectUriNotRegistered_ThrowsWithoutRedirect()
    {
        Assert.ThrowsAsync<OAuthProtocolException>(() => CreateStartAuthorizationHandler().Handle(
            Authorize(redirectUri: "https://evil.test/callback"),
            CancellationToken.None));
    }

    [Test]
    public async Task StartAuthorization_WhenPkceMissing_RedirectsErrorWithStateToClient()
    {
        var result = await CreateStartAuthorizationHandler().Handle(Authorize(codeChallenge: null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.RedirectUrl, Does.StartWith(Callback + "?"));
            Assert.That(result.RedirectUrl, Does.Contain("error=invalid_request"));
            Assert.That(result.RedirectUrl, Does.Contain("state=client-state"));
        });
        _requests.Verify(x => x.Add(It.IsAny<AuthorizationRequest>()), Times.Never);
    }

    [Test]
    public async Task StartAuthorization_WithoutSso_StoresRequestAndRedirectsToLoginUi()
    {
        AuthorizationRequest? stored = null;
        _requests.Setup(x => x.Add(It.IsAny<AuthorizationRequest>())).Callback<AuthorizationRequest>(x => stored = x);

        var result = await CreateStartAuthorizationHandler().Handle(Authorize(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.CodeChallenge?.Value, Is.EqualTo(Challenge));
            Assert.That(stored.ExpiresAt, Is.EqualTo(Now.AddMinutes(10)));
            Assert.That(result.RedirectUrl, Is.EqualTo($"https://identity.test/login?request={stored.Id.Value}"));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task StartAuthorization_WithActiveSso_IssuesCodeWithoutLoginPage()
    {
        var session = CreateSession();
        _sessions.Setup(x => x.GetBySsoTokenHashAsync(new TokenHash("hash:sso-token"), CancellationToken.None)).ReturnsAsync(session);
        _tokenGenerator.Setup(x => x.GenerateToken(32)).Returns("silent-code");

        var result = await CreateStartAuthorizationHandler().Handle(Authorize(ssoToken: "sso-token"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.RedirectUrl, Does.StartWith(Callback + "?"));
            Assert.That(result.RedirectUrl, Does.Contain("code=silent-code"));
            Assert.That(result.RedirectUrl, Does.Contain("state=client-state"));
            Assert.That(session.AuthCodes.Single().ClientId.Value, Is.EqualTo(ClientIdValue));
        });
        _requests.Verify(x => x.Add(It.IsAny<AuthorizationRequest>()), Times.Never);
    }

    [Test]
    public async Task StartExternalLogin_WhenRequestPending_LinksOAuthStateToRequest()
    {
        var request = CreateRequest();
        var oauthStates = new Mock<IOAuthStateRepository>();
        OAuthState? stored = null;
        oauthStates.Setup(x => x.Add(It.IsAny<OAuthState>())).Callback<OAuthState>(x => stored = x);
        _requests.Setup(x => x.GetByIdAsync(request.Id, CancellationToken.None)).ReturnsAsync(request);
        _tokenGenerator.SetupSequence(x => x.GenerateToken(It.IsAny<int>()))
            .Returns("provider-state")
            .Returns("provider-verifier")
            .Returns("provider-nonce");

        var url = await CreateStartExternalLoginHandler(oauthStates).Handle(
            new StartExternalLoginCommand("google", request.Id.Value),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(stored!.AuthorizationRequestId, Is.EqualTo(request.Id));
            Assert.That(url, Does.StartWith("https://provider.test/auth?"));
            Assert.That(url, Does.Contain("state=provider-state"));
        });
    }

    [Test]
    public void StartExternalLogin_WhenRequestExpired_Throws()
    {
        var request = AuthorizationRequest.Create(
            new ClientId(ClientIdValue), new RedirectUri(Callback), new ClientState("s"), new CodeChallenge(Challenge), Now.AddMinutes(-20), Now.AddMinutes(-10));
        _requests.Setup(x => x.GetByIdAsync(request.Id, CancellationToken.None)).ReturnsAsync(request);

        Assert.ThrowsAsync<AuthorizationRequestExpiredException>(() => CreateStartExternalLoginHandler(new Mock<IOAuthStateRepository>())
            .Handle(new StartExternalLoginCommand("google", request.Id.Value), CancellationToken.None));
    }

    [Test]
    public async Task HandleOAuthCallback_WhenUserIsNew_StartsSsoSessionCompletesRequestAndRedirectsWithCode()
    {
        var request = CreateRequest();
        var oauthState = OAuthState.Create(
            new OAuthStateValue("provider-state"), new CodeVerifier("provider-verifier"), new Nonce("expected-nonce"), Now.AddMinutes(5), request.Id);
        var oauthStates = new Mock<IOAuthStateRepository>();
        var users = new Mock<IUserRepository>();
        Session? createdSession = null;

        oauthStates.Setup(x => x.GetByStateAsync(new OAuthStateValue("provider-state"), CancellationToken.None)).ReturnsAsync(oauthState);
        _requests.Setup(x => x.GetByIdAsync(request.Id, CancellationToken.None)).ReturnsAsync(request);
        users.Setup(x => x.GetByExternalIdentityAsync(It.IsAny<ProviderId>(), It.IsAny<ProviderUserId>(), CancellationToken.None)).ReturnsAsync((User?)null);
        users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), CancellationToken.None)).ReturnsAsync((User?)null);
        _sessions.Setup(x => x.Add(It.IsAny<Session>())).Callback<Session>(x => createdSession = x);
        _tokenGenerator.SetupSequence(x => x.GenerateToken(32)).Returns("sso-token").Returns("auth-code");

        var handler = new HandleOAuthCallbackHandler(
            oauthStates.Object,
            _requests.Object,
            users.Object,
            _sessions.Object,
            _unitOfWork.Object,
            [new TestProvider("google", "Google", true, new ExternalIdentityInfo("subject-1", "user@example.com", true, "Test User", "expected-nonce"))],
            new Mock<ISignInSettingsRepository>().Object,
            _tokenGenerator.Object,
            _tokenHasher.Object,
            Clock(),
            _options,
            Logger());

        var result = await handler.Handle(
            new HandleOAuthCallbackCommand("google", "provider-code", "provider-state", "unit-test", "127.0.0.1"),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.RedirectUrl, Is.EqualTo($"{Callback}?code=auth-code&state=client-state"));
            Assert.That(result.SsoToken, Is.EqualTo("sso-token"));
            Assert.That(result.SsoExpiresAt, Is.EqualTo(Now.AddHours(12)));
            Assert.That(createdSession!.SsoTokenHash.Value, Is.EqualTo("hash:sso-token"));
            Assert.That(request.IsCompleted, Is.True);
        });
        oauthStates.Verify(x => x.Remove(oauthState), Times.Once);
    }

    [Test]
    public async Task ExchangeAuthCode_WhenClientAndPkceValid_ReturnsTokens()
    {
        var (session, user, users, jwt) = ArrangeExchange();
        _tokenGenerator.Setup(x => x.GenerateToken(64)).Returns("refresh-token");

        var result = await CreateExchangeHandler(users, jwt).Handle(
            new ExchangeAuthCodeCommand("auth-code", ClientIdValue, ClientSecret, Callback, Verifier),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.AccessToken, Is.EqualTo("access-token"));
            Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));
            Assert.That(result.RefreshExpiresInSeconds, Is.EqualTo(14 * 24 * 3600));
            Assert.That(result.SessionId, Is.EqualTo(session.Id.Value));
            Assert.That(result.User.Email, Is.EqualTo(user.Email.Value));
        });
    }

    [Test]
    public void ExchangeAuthCode_WhenClientSecretWrong_ReturnsInvalidClient()
    {
        var (_, _, users, jwt) = ArrangeExchange();

        var ex = Assert.ThrowsAsync<OAuthProtocolException>(() => CreateExchangeHandler(users, jwt).Handle(
            new ExchangeAuthCodeCommand("auth-code", ClientIdValue, "wrong", Callback, Verifier),
            CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Error, Is.EqualTo(OAuthError.InvalidClient));
            Assert.That(ex.StatusCode, Is.EqualTo(401));
        });
    }

    [Test]
    public void ExchangeAuthCode_WhenVerifierWrong_ReturnsInvalidGrantAndPersistsBurnedCode()
    {
        var (session, _, users, jwt) = ArrangeExchange();

        var ex = Assert.ThrowsAsync<OAuthProtocolException>(() => CreateExchangeHandler(users, jwt).Handle(
            new ExchangeAuthCodeCommand("auth-code", ClientIdValue, ClientSecret, Callback, "wrong-verifier-wrong-verifier-wrong-verifier-1"),
            CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Error, Is.EqualTo(OAuthError.InvalidGrant));
            Assert.That(session.AuthCodes.Single().IsUsed, Is.True);
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void RefreshSession_WhenClientUnauthenticated_ReturnsInvalidClient()
    {
        var handler = new RefreshSessionHandler(
            _clients, _sessions.Object, new Mock<IUserRepository>().Object, _unitOfWork.Object,
            new Mock<IJwtTokenService>().Object, _tokenGenerator.Object, _tokenHasher.Object, Clock(), _options, Logger());

        var ex = Assert.ThrowsAsync<OAuthProtocolException>(() => handler.Handle(
            new RefreshSessionCommand("refresh-token", ClientIdValue, null),
            CancellationToken.None));

        Assert.That(ex!.Error, Is.EqualTo(OAuthError.InvalidClient));
        _sessions.Verify(x => x.GetByRefreshTokenHashAsync(It.IsAny<TokenHash>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase("https://app.test/", "https://app.test/")]
    [TestCase("https://evil.test/", "https://identity.test/logged-out")]
    [TestCase(null, "https://identity.test/logged-out")]
    public async Task EndSession_RevokesSsoSessionAndOnlyHonoursRegisteredPostLogoutUris(string? postLogoutUri, string expected)
    {
        var session = CreateSession();
        _sessions.Setup(x => x.GetBySsoTokenHashAsync(new TokenHash("hash:sso-token"), CancellationToken.None)).ReturnsAsync(session);
        var handler = new EndSessionHandler(_clients, _sessions.Object, _unitOfWork.Object, _tokenHasher.Object, Clock(), _options, Logger());

        var result = await handler.Handle(new EndSessionCommand("sso-token", ClientIdValue, postLogoutUri), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.RedirectUrl, Is.EqualTo(expected));
            Assert.That(session.IsRevoked, Is.True);
        });
    }

    [Test]
    public async Task GetAuthorizationRequest_ListsEnabledProvidersOnlyWhilePending()
    {
        var pendingId = Guid.NewGuid();
        var expiredId = Guid.NewGuid();
        var readService = new Mock<IAuthorizationRequestReadService>();
        readService.Setup(x => x.GetAsync(pendingId, CancellationToken.None))
            .ReturnsAsync(new AuthorizationRequestReadModel(pendingId, ClientIdValue, Now.AddMinutes(5), null));
        readService.Setup(x => x.GetAsync(expiredId, CancellationToken.None))
            .ReturnsAsync(new AuthorizationRequestReadModel(expiredId, ClientIdValue, Now.AddMinutes(-1), null));
        var handler = new GetAuthorizationRequestHandler(
            readService.Object,
            _clients,
            [new TestProvider("google", "Google", true), new TestProvider("github", "GitHub", false)],
            NoDisabledMethods(),
            Clock());

        var pending = await handler.Handle(new GetAuthorizationRequestQuery(pendingId), CancellationToken.None);
        var expired = await handler.Handle(new GetAuthorizationRequestQuery(expiredId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(pending!.Status, Is.EqualTo(AuthorizationRequestStatus.Pending));
            Assert.That(pending.ClientName, Is.EqualTo("Vaultly"));
            Assert.That(pending.Providers.Single().StartUrl, Is.EqualTo($"/google/authorize?request={pendingId}"));
            Assert.That(expired!.Status, Is.EqualTo(AuthorizationRequestStatus.Expired));
            Assert.That(expired.Providers, Is.Empty);
        });
    }

    private StartAuthorizationHandler CreateStartAuthorizationHandler()
        => new(_clients, _requests.Object, _sessions.Object, _unitOfWork.Object, _tokenGenerator.Object, _tokenHasher.Object, Clock(), _options, Logger());

    private StartExternalLoginHandler CreateStartExternalLoginHandler(Mock<IOAuthStateRepository> oauthStates)
        => new(oauthStates.Object, _requests.Object, _unitOfWork.Object, [new TestProvider("google", "Google", true)], new Mock<ISignInSettingsRepository>().Object, _tokenGenerator.Object, Clock(), _options, Logger());

    internal static ISignInSettingsReadService NoDisabledMethods()
    {
        var readService = new Mock<ISignInSettingsReadService>();
        readService.Setup(x => x.GetDisabledMethodsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<string>());
        return readService.Object;
    }

    private ExchangeAuthCodeHandler CreateExchangeHandler(Mock<IUserRepository> users, Mock<IJwtTokenService> jwt)
        => new(_clients, _sessions.Object, users.Object, _unitOfWork.Object, jwt.Object, _tokenGenerator.Object, _tokenHasher.Object, Clock(), _options, Logger());

    private (Session Session, User User, Mock<IUserRepository> Users, Mock<IJwtTokenService> Jwt) ArrangeExchange()
    {
        var session = CreateSession();
        session.IssueAuthCode(new CodeHash("hash:auth-code"), Now.AddMinutes(2), new ClientId(ClientIdValue), new RedirectUri(Callback), new CodeChallenge(Challenge));
        var user = User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), true, Now);
        var users = new Mock<IUserRepository>();
        var jwt = new Mock<IJwtTokenService>();
        _sessions.Setup(x => x.GetByAuthCodeHashAsync(new CodeHash("hash:auth-code"), CancellationToken.None)).ReturnsAsync(session);
        users.Setup(x => x.GetByIdAsync(session.UserId, CancellationToken.None)).ReturnsAsync(user);
        jwt.Setup(x => x.CreateAccessToken(user, session, Now)).Returns("access-token");
        return (session, user, users, jwt);
    }

    private static StartAuthorizationCommand Authorize(
        string clientId = ClientIdValue,
        string redirectUri = Callback,
        string? codeChallenge = Challenge,
        string? ssoToken = null)
        => new(clientId, redirectUri, "code", "client-state", codeChallenge, codeChallenge is null ? null : "S256", ssoToken);

    private static AuthorizationRequest CreateRequest()
        => AuthorizationRequest.Create(
            new ClientId(ClientIdValue), new RedirectUri(Callback), new ClientState("client-state"), new CodeChallenge(Challenge), Now, Now.AddMinutes(10));

    private static Session CreateSession()
        => Session.Create(UserId.New(), new AuthMethod("google"), null, null, new TokenHash("hash:sso-token"), Now.AddHours(-1), Now.AddHours(11));

    private static IClock Clock()
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(Now);
        return clock.Object;
    }

    private static ILogger Logger() => new LoggerConfiguration().CreateLogger();

    private sealed class TestProvider(
        string id,
        string name,
        bool enabled,
        ExternalIdentityInfo? exchangeResult = null) : IExternalOAuthProvider
    {
        public ProviderId Id { get; } = new(id);
        public string Name { get; } = name;
        public bool IsConfigured { get; } = enabled;
        public string AuthorizationEndpoint { get; } = "https://provider.test/auth";
        public string ClientId { get; } = "client";
        public string RedirectUri { get; } = "https://identity.test/callback";
        public IReadOnlyList<string> Scopes { get; } = ["openid", "email", "profile"];

        public Task<ExternalIdentityInfo> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
            => exchangeResult is null
                ? throw new NotSupportedException("ExchangeCodeAsync is not used in this test.")
                : Task.FromResult(exchangeResult);
    }
}
