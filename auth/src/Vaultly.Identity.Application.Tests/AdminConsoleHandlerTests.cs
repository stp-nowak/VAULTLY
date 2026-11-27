using Moq;

using NUnit.Framework;

using Serilog;

using Vaultly.Core;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;
using Vaultly.Identity.Application.Features.Authorization.StartConsoleSignIn;
using Vaultly.Identity.Application.Features.ExternalLogin;
using Vaultly.Identity.Application.Features.ExternalLogin.HandleOAuthCallback;
using Vaultly.Identity.Application.Features.Users;
using Vaultly.Identity.Application.Features.Users.AssignUserRole;
using Vaultly.Identity.Application.Features.Users.EnsureBootstrapAdmin;
using Vaultly.Identity.Application.Features.Users.GetConsoleUser;
using Vaultly.Identity.Application.Features.Users.GetUsers;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.OAuthStates;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class AdminConsoleHandlerTests
{
    private const string ConsoleUrl = "https://login.test/admin";

    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private Mock<IAuthorizationRequestRepository> _requests = null!;
    private Mock<ISessionRepository> _sessions = null!;
    private Mock<IUserRepository> _users = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITokenHasher> _tokenHasher = null!;
    private IdentityOptions _identityOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _requests = new Mock<IAuthorizationRequestRepository>();
        _sessions = new Mock<ISessionRepository>();
        _users = new Mock<IUserRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tokenHasher = new Mock<ITokenHasher>();
        _tokenHasher.Setup(x => x.HashToken(It.IsAny<string>())).Returns<string>(value => $"hash:{value}");
        _identityOptions = new IdentityOptions
        {
            PublicBaseUrl = "https://identity.test",
            LoginUiBaseUrl = "https://login.test",
            AuthCodeTtlMinutes = 2,
            AuthorizationRequestTtlMinutes = 10,
            SsoSessionTtlHours = 12,
        };
    }

    [Test]
    public async Task StartConsoleSignIn_WithoutSso_StoresConsoleRequestAndOpensLoginPage()
    {
        AuthorizationRequest? stored = null;
        _requests.Setup(x => x.Add(It.IsAny<AuthorizationRequest>())).Callback<AuthorizationRequest>(x => stored = x);

        var result = await CreateStartConsoleSignInHandler().Handle(new StartConsoleSignInCommand(null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(stored!.IsForConsole, Is.True);
            Assert.That(stored.RedirectUri.Value, Is.EqualTo(ConsoleUrl));
            Assert.That(stored.ExpiresAt, Is.EqualTo(Now.AddMinutes(10)));
            Assert.That(result.RedirectUrl, Is.EqualTo($"https://login.test/login?request={stored.Id.Value}"));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task StartConsoleSignIn_WithActiveSso_OpensConsoleDirectly()
    {
        _sessions.Setup(x => x.GetBySsoTokenHashAsync(new TokenHash("hash:sso-token"), CancellationToken.None))
            .ReturnsAsync(CreateSession(ssoExpiresAt: Now.AddHours(1)));

        var result = await CreateStartConsoleSignInHandler().Handle(new StartConsoleSignInCommand("sso-token"), CancellationToken.None);

        Assert.That(result.RedirectUrl, Is.EqualTo(ConsoleUrl));
        _requests.Verify(x => x.Add(It.IsAny<AuthorizationRequest>()), Times.Never);
    }

    [Test]
    public async Task StartConsoleSignIn_WithExpiredSso_OpensLoginPage()
    {
        _sessions.Setup(x => x.GetBySsoTokenHashAsync(new TokenHash("hash:sso-token"), CancellationToken.None))
            .ReturnsAsync(CreateSession(ssoExpiresAt: Now.AddMinutes(-1)));

        var result = await CreateStartConsoleSignInHandler().Handle(new StartConsoleSignInCommand("sso-token"), CancellationToken.None);

        Assert.That(result.RedirectUrl, Does.StartWith("https://login.test/login?request="));
    }

    [Test]
    public async Task HandleOAuthCallback_ForConsoleRequest_StartsSsoSessionAndReturnsToConsoleWithoutCode()
    {
        var request = AuthorizationRequest.CreateForConsole(new RedirectUri(ConsoleUrl), Now, Now.AddMinutes(10));
        var oauthState = OAuthState.Create(
            new OAuthStateValue("provider-state"), new CodeVerifier("provider-verifier"), new Nonce("nonce"), Now.AddMinutes(5), request.Id);
        var oauthStates = new Mock<IOAuthStateRepository>();
        var tokenGenerator = new Mock<ITokenGenerator>();
        Session? session = null;
        oauthStates.Setup(x => x.GetByStateAsync(new OAuthStateValue("provider-state"), CancellationToken.None)).ReturnsAsync(oauthState);
        _requests.Setup(x => x.GetByIdAsync(request.Id, CancellationToken.None)).ReturnsAsync(request);
        _sessions.Setup(x => x.Add(It.IsAny<Session>())).Callback<Session>(x => session = x);
        tokenGenerator.Setup(x => x.GenerateToken(32)).Returns("sso-token");

        var handler = new HandleOAuthCallbackHandler(
            oauthStates.Object,
            _requests.Object,
            _users.Object,
            _sessions.Object,
            _unitOfWork.Object,
            [new TestProvider(new ExternalIdentityInfo("subject-1", "admin@example.com", true, "Admin", "nonce"))],
            new Mock<ISignInSettingsRepository>().Object,
            tokenGenerator.Object,
            _tokenHasher.Object,
            Clock(),
            Microsoft.Extensions.Options.Options.Create(_identityOptions),
            Logger());

        var result = await handler.Handle(
            new HandleOAuthCallbackCommand("google", "code", "provider-state", null, null),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.RedirectUrl, Is.EqualTo(ConsoleUrl));
            Assert.That(result.SsoToken, Is.EqualTo("sso-token"));
            Assert.That(session!.AuthCodes, Is.Empty);
            Assert.That(request.IsCompleted, Is.True);
        });
    }

    [Test]
    public async Task AssignUserRole_PromotesUserAndSaves()
    {
        var user = CreateUser();
        _users.Setup(x => x.GetByIdAsync(user.Id, CancellationToken.None)).ReturnsAsync(user);
        _users.Setup(x => x.CountAdminsAsync(CancellationToken.None)).ReturnsAsync(1);

        var assigned = await CreateAssignUserRoleHandler().Handle(
            new AssignUserRoleCommand(Guid.NewGuid(), user.Id.Value, UserRole.Admin),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(assigned, Is.True);
            Assert.That(user.IsAdmin, Is.True);
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void AssignUserRole_WhenDemotingLastAdmin_ThrowsWithoutSaving()
    {
        var admin = CreateUser();
        RoleAssignment.Assign(admin, UserRole.Admin, adminCount: 0);
        _users.Setup(x => x.GetByIdAsync(admin.Id, CancellationToken.None)).ReturnsAsync(admin);
        _users.Setup(x => x.CountAdminsAsync(CancellationToken.None)).ReturnsAsync(1);

        Assert.ThrowsAsync<LastAdminException>(() => CreateAssignUserRoleHandler().Handle(
            new AssignUserRoleCommand(admin.Id.Value, admin.Id.Value, UserRole.User),
            CancellationToken.None));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AssignUserRole_WhenUserUnknown_ReturnsFalse()
    {
        _users.Setup(x => x.GetByIdAsync(It.IsAny<UserId>(), CancellationToken.None)).ReturnsAsync((User?)null);

        var assigned = await CreateAssignUserRoleHandler().Handle(
            new AssignUserRoleCommand(Guid.NewGuid(), Guid.NewGuid(), UserRole.Admin),
            CancellationToken.None);

        Assert.That(assigned, Is.False);
    }

    [Test]
    public async Task EnsureBootstrapAdmin_WithoutAdmins_CreatesVerifiedAdminForConfiguredEmail()
    {
        _identityOptions.BootstrapAdmin.Email = "Owner@Example.com";
        User? created = null;
        _users.Setup(x => x.CountAdminsAsync(CancellationToken.None)).ReturnsAsync(0);
        _users.Setup(x => x.GetByEmailAsync(new EmailAddress("owner@example.com"), CancellationToken.None)).ReturnsAsync((User?)null);
        _users.Setup(x => x.Add(It.IsAny<User>())).Callback<User>(x => created = x);

        await CreateEnsureBootstrapAdminHandler().Handle(new EnsureBootstrapAdminCommand(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(created!.Email.Value, Is.EqualTo("owner@example.com"));
            Assert.That(created.Name.Value, Is.EqualTo("owner"));
            Assert.That(created.EmailVerified, Is.True);
            Assert.That(created.IsAdmin, Is.True);
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task EnsureBootstrapAdmin_WithoutAdmins_PromotesExistingUser()
    {
        _identityOptions.BootstrapAdmin.Email = "user@example.com";
        var user = CreateUser();
        _users.Setup(x => x.CountAdminsAsync(CancellationToken.None)).ReturnsAsync(0);
        _users.Setup(x => x.GetByEmailAsync(user.Email, CancellationToken.None)).ReturnsAsync(user);

        await CreateEnsureBootstrapAdminHandler().Handle(new EnsureBootstrapAdminCommand(), CancellationToken.None);

        Assert.That(user.IsAdmin, Is.True);
        _users.Verify(x => x.Add(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task EnsureBootstrapAdmin_WithAConfiguredPassword_GivesTheAdminThatPassword()
    {
        _identityOptions.BootstrapAdmin.Email = "owner@example.com";
        _identityOptions.BootstrapAdmin.Password = "correct horse battery staple";
        User? created = null;
        _users.Setup(x => x.CountAdminsAsync(CancellationToken.None)).ReturnsAsync(0);
        _users.Setup(x => x.Add(It.IsAny<User>())).Callback<User>(x => created = x);

        await CreateEnsureBootstrapAdminHandler().Handle(new EnsureBootstrapAdminCommand(), CancellationToken.None);

        var verification = new Infrastructure.Security.IdentityPasswordHasher()
            .Verify(created!.PasswordHash!, new Password("correct horse battery staple"));
        Assert.That(verification, Is.EqualTo(PasswordVerification.Succeeded));
    }

    [TestCase("owner@example.com", 1)]
    [TestCase("", 0)]
    public async Task EnsureBootstrapAdmin_WhenAdminExistsOrNoEmailConfigured_DoesNothing(string email, int adminCount)
    {
        _identityOptions.BootstrapAdmin.Email = email;
        _users.Setup(x => x.CountAdminsAsync(CancellationToken.None)).ReturnsAsync(adminCount);

        await CreateEnsureBootstrapAdminHandler().Handle(new EnsureBootstrapAdminCommand(), CancellationToken.None);

        _users.Verify(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(0, 0, 1, 1)]
    [TestCase(3, 500, 3, GetUsersHandler.MaxPageSize)]
    public async Task GetUsers_ClampsPagingAndTrimsSearch(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var readService = new Mock<IUserReadService>();
        readService.Setup(x => x.GetPageAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), CancellationToken.None))
            .ReturnsAsync(new UserPageDto([], 0, expectedPage, expectedPageSize));

        await new GetUsersHandler(readService.Object).Handle(new GetUsersQuery("  ada ", page, pageSize), CancellationToken.None);

        readService.Verify(x => x.GetPageAsync("ada", expectedPage, expectedPageSize, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetUsers_WithBlankSearch_ListsEveryone()
    {
        var readService = new Mock<IUserReadService>();
        readService.Setup(x => x.GetPageAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), CancellationToken.None))
            .ReturnsAsync(new UserPageDto([], 0, 1, 20));

        await new GetUsersHandler(readService.Object).Handle(new GetUsersQuery("   ", 1, 20), CancellationToken.None);

        readService.Verify(x => x.GetPageAsync(null, 1, 20, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetConsoleUser_LooksUpTheSessionByHashedSsoToken()
    {
        var readService = new Mock<IUserReadService>();
        var admin = new ConsoleUserDto(Guid.NewGuid(), "admin@example.com", "Admin", UserRole.Admin);
        readService.Setup(x => x.GetBySsoTokenHashAsync("hash:sso-token", Now, CancellationToken.None)).ReturnsAsync(admin);
        var handler = new GetConsoleUserHandler(readService.Object, _tokenHasher.Object, Clock());

        Assert.Multiple(async () =>
        {
            Assert.That(await handler.Handle(new GetConsoleUserQuery("sso-token"), CancellationToken.None), Is.EqualTo(admin));
            Assert.That(await handler.Handle(new GetConsoleUserQuery(null), CancellationToken.None), Is.Null);
        });
    }

    [Test]
    public async Task GetAuthorizationRequest_ForConsole_NamesTheAdminConsole()
    {
        var id = Guid.NewGuid();
        var readService = new Mock<IAuthorizationRequestReadService>();
        readService.Setup(x => x.GetAsync(id, CancellationToken.None))
            .ReturnsAsync(new AuthorizationRequestReadModel(id, ConsoleClient.Id.Value, Now.AddMinutes(5), null));
        var handler = new GetAuthorizationRequestHandler(
            readService.Object,
            new ClientRegistry(Microsoft.Extensions.Options.Options.Create(_identityOptions)),
            [],
            IdentityUseCaseHandlerTests.NoDisabledMethods(),
            Clock());

        var result = await handler.Handle(new GetAuthorizationRequestQuery(id), CancellationToken.None);

        Assert.That(result!.ClientName, Is.EqualTo(GetAuthorizationRequestHandler.ConsoleDisplayName));
    }

    [Test]
    public void ClientRegistry_RejectsAClientUsingTheConsoleId()
    {
        _identityOptions.Clients =
        [
            new OAuthClientOptions
            {
                ClientId = ConsoleClient.Id.Value,
                ClientSecret = "secret",
                RedirectUris = ["https://app.test/callback"],
            },
        ];

        Assert.That(
            () => new ClientRegistry(Microsoft.Extensions.Options.Options.Create(_identityOptions)),
            Throws.TypeOf<InvalidOperationException>());
    }

    private StartConsoleSignInHandler CreateStartConsoleSignInHandler()
        => new(_requests.Object, _sessions.Object, _unitOfWork.Object, _tokenHasher.Object, Clock(), Microsoft.Extensions.Options.Options.Create(_identityOptions), Logger());

    private AssignUserRoleHandler CreateAssignUserRoleHandler() => new(_users.Object, _unitOfWork.Object, Logger());

    private EnsureBootstrapAdminHandler CreateEnsureBootstrapAdminHandler()
        => new(_users.Object, _unitOfWork.Object, new Infrastructure.Security.IdentityPasswordHasher(), Clock(), Microsoft.Extensions.Options.Options.Create(_identityOptions), Logger());

    private static User CreateUser() => User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), true, Now);

    private static Session CreateSession(DateTimeOffset ssoExpiresAt)
        => Session.Create(UserId.New(), new AuthMethod("google"), null, null, new TokenHash("hash:sso-token"), ssoExpiresAt.AddHours(-12), ssoExpiresAt);

    private static TimeProvider Clock() => new FixedTime(Now);

    private static ILogger Logger() => new LoggerConfiguration().CreateLogger();

    private sealed class TestProvider(ExternalIdentityInfo exchangeResult) : IExternalOAuthProvider
    {
        public ProviderId Id { get; } = new("google");
        public string Name => "Google";
        public bool IsConfigured => true;
        public string AuthorizationEndpoint => "https://provider.test/auth";
        public string ClientId => "client";
        public string RedirectUri => "https://identity.test/callback";
        public IReadOnlyList<string> Scopes { get; } = ["openid"];

        public Task<ExternalIdentityInfo> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
            => Task.FromResult(exchangeResult);
    }
}
