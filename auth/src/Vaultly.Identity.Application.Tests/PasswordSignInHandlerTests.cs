using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Serilog;

using Vaultly.Identity.Application.Commands.HandleOAuthCallback;
using Vaultly.Identity.Application.Commands.RegisterWithPassword;
using Vaultly.Identity.Application.Commands.SignInWithPassword;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Queries.GetAuthorizationRequest;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.Infrastructure.Security;
using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class PasswordSignInHandlerTests
{
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
    private const string Callback = "https://app.test/api/auth/callback";
    private const string GoodPassword = "correct horse battery staple";

    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly IdentityPasswordHasher Hasher = new();

    private Mock<IAuthorizationRequestRepository> _requests = null!;
    private Mock<IUserRepository> _users = null!;
    private Mock<ISessionRepository> _sessions = null!;
    private Mock<ISignInSettingsRepository> _settings = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITokenGenerator> _tokenGenerator = null!;
    private AuthorizationRequest _request = null!;
    private Session? _startedSession;

    [SetUp]
    public void SetUp()
    {
        _requests = new Mock<IAuthorizationRequestRepository>();
        _users = new Mock<IUserRepository>();
        _sessions = new Mock<ISessionRepository>();
        _settings = new Mock<ISignInSettingsRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tokenGenerator = new Mock<ITokenGenerator>();
        _tokenGenerator.SetupSequence(x => x.GenerateToken(32)).Returns("sso-token").Returns("auth-code");
        _request = AuthorizationRequest.Create(
            new ClientId("vaultly-web"), new RedirectUri(Callback), new ClientState("client-state"), new CodeChallenge(Challenge), Now, Now.AddMinutes(10));
        _requests.Setup(x => x.GetByIdAsync(_request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_request);
        _sessions.Setup(x => x.Add(It.IsAny<Session>())).Callback<Session>(x => _startedSession = x);
        _startedSession = null;
    }

    [Test]
    public async Task SignIn_WithTheRightPassword_StartsAPasswordSessionAndRedirectsWithACode()
    {
        var user = UserWithPassword();
        user.RecordFailedSignIn(Now);

        var result = await CreateSignInHandler().Handle(SignIn("ada@example.com", GoodPassword), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.RedirectUrl, Is.EqualTo($"{Callback}?code=auth-code&state=client-state"));
            Assert.That(result.SsoToken, Is.EqualTo("sso-token"));
            Assert.That(_startedSession!.AuthMethod.Value, Is.EqualTo("password"));
            Assert.That(_startedSession.UserId, Is.EqualTo(user.Id));
            Assert.That(user.FailedSignInCount, Is.Zero);
            Assert.That(_request.IsCompleted, Is.True);
        });
    }

    [Test]
    public void SignIn_WithAWrongPassword_CountsTheFailureAndRefuses()
    {
        var user = UserWithPassword();

        var ex = Assert.ThrowsAsync<PasswordSignInException>(() => CreateSignInHandler().Handle(
            SignIn("ada@example.com", "wrong password here"), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Failure, Is.EqualTo(PasswordSignInFailure.InvalidCredentials));
            Assert.That(user.FailedSignInCount, Is.EqualTo(1));
            Assert.That(_request.IsCompleted, Is.False);
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void SignIn_WhileLockedOut_RefusesEvenTheRightPassword()
    {
        var user = UserWithPassword();
        for (var attempt = 0; attempt < User.MaxFailedSignIns; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }

        var ex = Assert.ThrowsAsync<PasswordSignInException>(() => CreateSignInHandler().Handle(
            SignIn("ada@example.com", GoodPassword), CancellationToken.None));

        Assert.That(ex!.Failure, Is.EqualTo(PasswordSignInFailure.InvalidCredentials));
        _sessions.Verify(x => x.Add(It.IsAny<Session>()), Times.Never);
    }

    [TestCase("nobody@example.com", GoodPassword)]
    [TestCase("not-an-email", GoodPassword)]
    [TestCase("ada@example.com", "short")]
    public void SignIn_WithUnknownOrMalformedCredentials_RefusesTheSameWay(string email, string password)
    {
        UserWithPassword();

        var ex = Assert.ThrowsAsync<PasswordSignInException>(() => CreateSignInHandler().Handle(SignIn(email, password), CancellationToken.None));

        Assert.That(ex!.Failure, Is.EqualTo(PasswordSignInFailure.InvalidCredentials));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void SignIn_ForAnAccountWithoutPassword_RefusesTheSameWay()
    {
        var user = User.Create(new EmailAddress("ada@example.com"), new PersonName("Ada"), true, Now);
        _users.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var ex = Assert.ThrowsAsync<PasswordSignInException>(() => CreateSignInHandler().Handle(
            SignIn("ada@example.com", GoodPassword), CancellationToken.None));

        Assert.That(ex!.Failure, Is.EqualTo(PasswordSignInFailure.InvalidCredentials));
    }

    [Test]
    public void SignIn_WhenPasswordSignInIsSwitchedOff_Throws()
    {
        UserWithPassword();
        SwitchPasswordOff();

        Assert.ThrowsAsync<InvalidOperationException>(() => CreateSignInHandler().Handle(
            SignIn("ada@example.com", GoodPassword), CancellationToken.None));
    }

    [Test]
    public async Task Register_CreatesAnUnverifiedUserAndSignsItIn()
    {
        User? created = null;
        _users.Setup(x => x.Add(It.IsAny<User>())).Callback<User>(x => created = x);

        var result = await CreateRegisterHandler().Handle(Register("Ada Lovelace", " Ada@Example.com "), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(created!.Email.Value, Is.EqualTo("ada@example.com"));
            Assert.That(created.EmailVerified, Is.False);
            Assert.That(Hasher.Verify(created.PasswordHash!, new Password(GoodPassword)), Is.EqualTo(PasswordVerification.Succeeded));
            Assert.That(_startedSession!.UserId, Is.EqualTo(created.Id));
            Assert.That(result.RedirectUrl, Does.StartWith(Callback));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Register_WithAnEmailInUse_RefusesWithoutCreatingAUser()
    {
        UserWithPassword();

        var ex = Assert.ThrowsAsync<PasswordSignInException>(() => CreateRegisterHandler().Handle(Register("Ada", "ada@example.com"), CancellationToken.None));

        Assert.That(ex!.Failure, Is.EqualTo(PasswordSignInFailure.EmailTaken));
        _users.Verify(x => x.Add(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void Register_WithAWeakPassword_Throws()
        => Assert.ThrowsAsync<WeakPasswordException>(() => CreateRegisterHandler().Handle(
            new RegisterWithPasswordCommand(_request.Id.Value, "Ada", "ada@example.com", "short", null, null),
            CancellationToken.None));

    [Test]
    public void Register_ForAnExpiredRequest_Throws()
    {
        var expired = AuthorizationRequest.Create(
            new ClientId("vaultly-web"), new RedirectUri(Callback), new ClientState("s"), new CodeChallenge(Challenge), Now.AddMinutes(-20), Now.AddMinutes(-10));
        _requests.Setup(x => x.GetByIdAsync(expired.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expired);

        Assert.ThrowsAsync<AuthorizationRequestExpiredException>(() => CreateRegisterHandler().Handle(
            new RegisterWithPasswordCommand(expired.Id.Value, "Ada", "ada@example.com", GoodPassword, null, null),
            CancellationToken.None));
    }

    [Test]
    public async Task ProviderSignIn_ToAnUnverifiedPasswordAccount_RemovesThePasswordAndEndsItsSessions()
    {
        var squatter = User.RegisterWithPassword(new EmailAddress("ada@example.com"), new PersonName("Not Ada"), Hasher.Hash(new Password(GoodPassword)), Now);
        var squatterSession = Session.Create(squatter.Id, new AuthMethod("password"), null, null, new TokenHash("old"), Now, Now.AddHours(1));
        var oauthState = OAuthState.Create(new OAuthStateValue("state"), new CodeVerifier("verifier"), new Nonce("nonce"), Now.AddMinutes(5), _request.Id);
        var oauthStates = new Mock<IOAuthStateRepository>();
        oauthStates.Setup(x => x.GetByStateAsync(new OAuthStateValue("state"), It.IsAny<CancellationToken>())).ReturnsAsync(oauthState);
        _users.Setup(x => x.GetByEmailAsync(squatter.Email, It.IsAny<CancellationToken>())).ReturnsAsync(squatter);
        _sessions.Setup(x => x.GetActiveForUserAsync(squatter.Id, It.IsAny<CancellationToken>())).ReturnsAsync([squatterSession]);
        var google = new Mock<IExternalOAuthProvider>();
        google.SetupGet(x => x.Id).Returns(new ProviderId("google"));
        google.SetupGet(x => x.IsConfigured).Returns(true);
        google.Setup(x => x.ExchangeCodeAsync("code", "verifier", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalIdentityInfo("google-subject", "ada@example.com", true, "Ada Lovelace", "nonce"));

        await new HandleOAuthCallbackHandler(
            oauthStates.Object, _requests.Object, _users.Object, _sessions.Object, _unitOfWork.Object, [google.Object],
            _settings.Object, _tokenGenerator.Object, TokenHasher(), Clock(), Options(), Logger())
            .Handle(new HandleOAuthCallbackCommand("google", "code", "state", null, null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(squatter.HasPassword, Is.False);
            Assert.That(squatter.EmailVerified, Is.True);
            Assert.That(squatterSession.IsRevoked, Is.True);
            Assert.That(_startedSession!.UserId, Is.EqualTo(squatter.Id));
        });
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public async Task GetAuthorizationRequest_OffersPasswordSignInUnlessSwitchedOff(bool switchedOff, bool expected)
    {
        var id = Guid.NewGuid();
        var requests = new Mock<IAuthorizationRequestReadService>();
        requests.Setup(x => x.GetAsync(id, CancellationToken.None))
            .ReturnsAsync(new AuthorizationRequestReadModel(id, "vaultly-web", Now.AddMinutes(5), null));
        var settings = new Mock<ISignInSettingsReadService>();
        settings.Setup(x => x.GetDisabledMethodsAsync(CancellationToken.None))
            .ReturnsAsync(switchedOff ? new HashSet<string> { "password" } : new HashSet<string>());

        var result = await new GetAuthorizationRequestHandler(
                requests.Object, new ClientRegistry(Options()), [], settings.Object, Clock())
            .Handle(new GetAuthorizationRequestQuery(id), CancellationToken.None);

        Assert.That(result!.PasswordSignIn, Is.EqualTo(expected));
    }

    private User UserWithPassword()
    {
        var user = User.RegisterWithPassword(new EmailAddress("ada@example.com"), new PersonName("Ada"), Hasher.Hash(new Password(GoodPassword)), Now);
        _users.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    private void SwitchPasswordOff()
    {
        var settings = SignInSettings.CreateDefault();
        settings.Disable(SignInMethod.Password, [new SignInMethod("google"), SignInMethod.Password]);
        _settings.Setup(x => x.FindAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
    }

    private SignInWithPasswordCommand SignIn(string email, string password) => new(_request.Id.Value, email, password, "unit-test", "127.0.0.1");

    private RegisterWithPasswordCommand Register(string name, string email) => new(_request.Id.Value, name, email, GoodPassword, null, null);

    private SignInWithPasswordHandler CreateSignInHandler()
        => new(_requests.Object, _users.Object, _sessions.Object, _settings.Object, _unitOfWork.Object, Hasher,
            _tokenGenerator.Object, TokenHasher(), Clock(), Options(), Logger());

    private RegisterWithPasswordHandler CreateRegisterHandler()
        => new(_requests.Object, _users.Object, _sessions.Object, _settings.Object, _unitOfWork.Object, Hasher,
            _tokenGenerator.Object, TokenHasher(), Clock(), Options(), Logger());

    private static IOptions<IdentityOptions> Options()
        => Microsoft.Extensions.Options.Options.Create(new IdentityOptions { AuthCodeTtlMinutes = 2, SsoSessionTtlHours = 12 });

    private static IClock Clock()
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(Now);
        return clock.Object;
    }

    private static ILogger Logger() => new LoggerConfiguration().CreateLogger();

    private static ITokenHasher TokenHasher()
    {
        var hasher = new Mock<ITokenHasher>();
        hasher.Setup(x => x.HashToken(It.IsAny<string>())).Returns<string>(value => $"hash:{value}");
        return hasher.Object;
    }
}
