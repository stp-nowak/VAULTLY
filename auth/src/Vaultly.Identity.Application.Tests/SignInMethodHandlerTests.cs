using Microsoft.EntityFrameworkCore;

using Moq;

using NUnit.Framework;

using Serilog;

using Vaultly.Identity.Application.Commands.SetSignInMethodEnabled;
using Vaultly.Identity.Application.Commands.StartExternalLogin;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Queries.GetAuthorizationRequest;
using Vaultly.Identity.Application.Queries.GetSignInSettings;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.Infrastructure.Persistence;
using Vaultly.Identity.Infrastructure.Persistence.ReadServices;
using Vaultly.Identity.Infrastructure.Persistence.Repositories;
using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class SignInMethodHandlerTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private Mock<ISignInSettingsRepository> _settings = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private SignInSettings? _stored;

    [SetUp]
    public void SetUp()
    {
        _stored = null;
        _settings = new Mock<ISignInSettingsRepository>();
        _settings.Setup(x => x.FindAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => _stored);
        _settings.Setup(x => x.Add(It.IsAny<SignInSettings>())).Callback<SignInSettings>(x => _stored = x);
        _unitOfWork = new Mock<IUnitOfWork>();
    }

    [Test]
    public void Catalog_ListsProvidersAndPasswordWithTheirConfiguration()
    {
        var catalog = Catalog(google: true, github: false);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.All.Select(x => (x.Method.Value, x.Name, x.IsConfigured)), Is.EqualTo(new[]
            {
                ("google", "Google", true),
                ("github", "GitHub", false),
                ("password", "Email and password", true),
            }));
            Assert.That(catalog.Configured, Is.EqualTo(new[] { new SignInMethod("google"), SignInMethod.Password }));
            Assert.That(catalog.Find(" GOOGLE ")?.Method.Value, Is.EqualTo("google"));
            Assert.That(catalog.Find("facebook"), Is.Null);
        });
    }

    [Test]
    public async Task SetSignInMethodEnabled_Disabling_StoresSettingsOnFirstChange()
    {
        var handler = CreateSetHandler(Catalog(google: true, github: true));

        var changed = await handler.Handle(new SetSignInMethodEnabledCommand(Guid.NewGuid(), "github", false), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(_stored!.IsEnabled(new SignInMethod("github")), Is.False);
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetSignInMethodEnabled_Enabling_TurnsAMethodBackOn()
    {
        _stored = SignInSettings.CreateDefault();
        _stored.Disable(new SignInMethod("github"), [new SignInMethod("google"), new SignInMethod("github")]);

        await CreateSetHandler(Catalog(google: true, github: true))
            .Handle(new SetSignInMethodEnabledCommand(Guid.NewGuid(), "github", true), CancellationToken.None);

        Assert.That(_stored.IsEnabled(new SignInMethod("github")), Is.True);
        _settings.Verify(x => x.Add(It.IsAny<SignInSettings>()), Times.Never);
    }

    [Test]
    public void SetSignInMethodEnabled_DisablingTheLastEnabledConfiguredMethod_ThrowsWithoutSaving()
    {
        _stored = SignInSettings.CreateDefault();
        _stored.Disable(SignInMethod.Password, [new SignInMethod("google"), SignInMethod.Password]);

        Assert.ThrowsAsync<LastSignInMethodException>(() => CreateSetHandler(Catalog(google: true, github: false))
            .Handle(new SetSignInMethodEnabledCommand(Guid.NewGuid(), "google", false), CancellationToken.None));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetSignInMethodEnabled_UnknownMethod_ReturnsFalse()
    {
        var changed = await CreateSetHandler(Catalog(google: true, github: true))
            .Handle(new SetSignInMethodEnabledCommand(Guid.NewGuid(), "facebook", false), CancellationToken.None);

        Assert.That(changed, Is.False);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetSignInSettings_CombinesConfigurationAndAdminChoices()
    {
        var readService = new Mock<ISignInSettingsReadService>();
        readService.Setup(x => x.GetDisabledMethodsAsync(CancellationToken.None)).ReturnsAsync(new HashSet<string> { "github" });

        var methods = await new GetSignInSettingsHandler(Catalog(google: true, github: true), readService.Object)
            .Handle(new GetSignInSettingsQuery(), CancellationToken.None);

        Assert.That(methods.Select(x => (x.Id, x.IsConfigured, x.IsEnabled)), Is.EqualTo(new[]
        {
            ("google", true, true),
            ("github", true, false),
            ("password", true, true),
        }));
    }

    [Test]
    public async Task GetAuthorizationRequest_HidesDisabledAndUnconfiguredProviders()
    {
        var id = Guid.NewGuid();
        var requests = new Mock<IAuthorizationRequestReadService>();
        requests.Setup(x => x.GetAsync(id, CancellationToken.None))
            .ReturnsAsync(new AuthorizationRequestReadModel(id, "vaultly-web", Now.AddMinutes(5), null));
        var settings = new Mock<ISignInSettingsReadService>();
        settings.Setup(x => x.GetDisabledMethodsAsync(CancellationToken.None)).ReturnsAsync(new HashSet<string> { "github" });
        var handler = new GetAuthorizationRequestHandler(
            requests.Object,
            new ClientRegistry(Microsoft.Extensions.Options.Options.Create(new IdentityOptions())),
            [Provider("google", true), Provider("github", true), Provider("gitlab", false)],
            settings.Object,
            Clock());

        var result = await handler.Handle(new GetAuthorizationRequestQuery(id), CancellationToken.None);

        Assert.That(result!.Providers.Select(x => x.Id), Is.EqualTo(new[] { "google" }));
    }

    [Test]
    public void StartExternalLogin_WithADisabledProvider_Throws()
    {
        _stored = SignInSettings.CreateDefault();
        _stored.Disable(new SignInMethod("google"), [new SignInMethod("google"), new SignInMethod("github")]);
        var handler = new StartExternalLoginHandler(
            new Mock<IOAuthStateRepository>().Object,
            new Mock<IAuthorizationRequestRepository>().Object,
            _unitOfWork.Object,
            [Provider("google", true)],
            _settings.Object,
            new Mock<ITokenGenerator>().Object,
            Clock(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new LoggerConfiguration().CreateLogger());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new StartExternalLoginCommand("google", Guid.NewGuid()),
            CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("Sign-in method is disabled."));
    }

    [Test]
    public async Task Persistence_StoresDisabledMethodsAndReadsThemBack()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using (var dbContext = new IdentityDbContext(options))
        {
            var settings = SignInSettings.CreateDefault();
            settings.Disable(new SignInMethod("github"), [new SignInMethod("google"), new SignInMethod("github")]);
            new SignInSettingsRepository(dbContext).Add(settings);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = new IdentityDbContext(options))
        {
            var disabled = await new SignInSettingsReadService(dbContext).GetDisabledMethodsAsync(CancellationToken.None);
            var reloaded = await new SignInSettingsRepository(dbContext).FindAsync(CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(disabled, Is.EquivalentTo(new[] { "github" }));
                Assert.That(reloaded!.IsEnabled(new SignInMethod("github")), Is.False);
                Assert.That(reloaded.IsEnabled(new SignInMethod("google")), Is.True);
            });
        }
    }

    [Test]
    public async Task Persistence_WithoutStoredSettings_HasNothingDisabled()
    {
        await using var dbContext = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

        Assert.That(await new SignInSettingsReadService(dbContext).GetDisabledMethodsAsync(CancellationToken.None), Is.Empty);
    }

    private SetSignInMethodEnabledHandler CreateSetHandler(SignInMethodCatalog catalog)
        => new(catalog, _settings.Object, _unitOfWork.Object, new LoggerConfiguration().CreateLogger());

    private static SignInMethodCatalog Catalog(bool google, bool github)
        => new([Provider("google", google, "Google"), Provider("github", github, "GitHub")]);

    private static IExternalOAuthProvider Provider(string id, bool configured, string? name = null)
    {
        var provider = new Mock<IExternalOAuthProvider>();
        provider.SetupGet(x => x.Id).Returns(new ProviderId(id));
        provider.SetupGet(x => x.Name).Returns(name ?? id);
        provider.SetupGet(x => x.IsConfigured).Returns(configured);
        return provider.Object;
    }

    private static IClock Clock()
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(Now);
        return clock.Object;
    }
}
