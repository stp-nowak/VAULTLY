using NUnit.Framework;

using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Domain.SignInMethods.Events;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class SignInSettingsTests
{
    private static readonly SignInMethod Google = new("google");
    private static readonly SignInMethod GitHub = new("github");

    [Test]
    public void CreateDefault_EnablesEveryMethod()
    {
        var settings = SignInSettings.CreateDefault();

        Assert.Multiple(() =>
        {
            Assert.That(settings.Id, Is.EqualTo(SignInSettings.SingletonId));
            Assert.That(settings.IsEnabled(Google), Is.True);
            Assert.That(settings.IsEnabled(SignInMethod.Password), Is.True);
        });
    }

    [Test]
    public void Disable_WhenAnotherAvailableMethodStaysEnabled_DisablesAndRaisesEvent()
    {
        var settings = SignInSettings.CreateDefault();

        settings.Disable(Google, [Google, GitHub]);

        Assert.Multiple(() =>
        {
            Assert.That(settings.IsEnabled(Google), Is.False);
            Assert.That(settings.DisabledMethods, Is.EqualTo(new[] { Google }));
            Assert.That(settings.DomainEvents.OfType<SignInMethodDisabled>().Single().Method, Is.EqualTo(Google));
        });
    }

    [Test]
    public void Disable_TheLastEnabledAvailableMethod_ThrowsAndKeepsItEnabled()
    {
        var settings = SignInSettings.CreateDefault();
        settings.Disable(GitHub, [Google, GitHub]);

        Assert.Multiple(() =>
        {
            Assert.That(() => settings.Disable(Google, [Google, GitHub]), Throws.TypeOf<LastSignInMethodException>());
            Assert.That(settings.IsEnabled(Google), Is.True);
        });
    }

    [Test]
    public void Disable_AMethodThatIsNotAvailable_IsAllowed()
    {
        var settings = SignInSettings.CreateDefault();

        settings.Disable(SignInMethod.Password, [Google]);

        Assert.That(settings.IsEnabled(SignInMethod.Password), Is.False);
    }

    [Test]
    public void Enable_DisabledMethod_EnablesAndRaisesEvent()
    {
        var settings = SignInSettings.CreateDefault();
        settings.Disable(Google, [Google, GitHub]);
        settings.ClearDomainEvents();

        settings.Enable(Google);

        Assert.Multiple(() =>
        {
            Assert.That(settings.IsEnabled(Google), Is.True);
            Assert.That(settings.DomainEvents.OfType<SignInMethodEnabled>().Single().Method, Is.EqualTo(Google));
        });
    }

    [Test]
    public void EnableOrDisable_WhenNothingChanges_RaisesNoEvent()
    {
        var settings = SignInSettings.CreateDefault();
        settings.Disable(Google, [Google, GitHub]);
        settings.ClearDomainEvents();

        settings.Enable(GitHub);
        settings.Disable(Google, [Google, GitHub]);

        Assert.That(settings.DomainEvents, Is.Empty);
    }

    [Test]
    public void SignInMethod_IsNormalizedLikeProviderIds()
        => Assert.That(new SignInMethod(" Google "), Is.EqualTo(Google));
}
