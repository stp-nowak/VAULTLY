using NUnit.Framework;

using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.Domain.Users.Passwords;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class UserPasswordTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void RegisterWithPassword_CreatesAnUnverifiedUserWithThePassword()
    {
        var user = Register();

        Assert.Multiple(() =>
        {
            Assert.That(user.HasPassword, Is.True);
            Assert.That(user.EmailVerified, Is.False);
            Assert.That(user.Role, Is.EqualTo(UserRole.User));
            Assert.That(user.DomainEvents.OfType<UserRegisteredDomainEvent>(), Has.Exactly(1).Items);
        });
    }

    [Test]
    public void RecordFailedSignIn_LocksAfterTooManyFailuresInARow()
    {
        var user = Register();

        for (var attempt = 1; attempt < User.MaxFailedSignIns; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }

        Assert.That(user.IsLockedOut(Now), Is.False);

        user.RecordFailedSignIn(Now);

        Assert.Multiple(() =>
        {
            Assert.That(user.IsLockedOut(Now), Is.True);
            Assert.That(user.IsLockedOut(Now + User.LockoutDuration), Is.False);
            Assert.That(user.DomainEvents.OfType<UserLockedOutDomainEvent>().Single().LockedUntil, Is.EqualTo(Now + User.LockoutDuration));
        });
    }

    [Test]
    public void RecordSuccessfulSignIn_ResetsTheFailureCount()
    {
        var user = Register();
        user.RecordFailedSignIn(Now);
        user.RecordFailedSignIn(Now);

        user.RecordSuccessfulSignIn();

        Assert.That(user.FailedSignInCount, Is.Zero);
    }

    [Test]
    public void SecureWithVerifiedEmail_ForAnUnverifiedAccount_RemovesThePasswordAndVerifies()
    {
        var user = Register();

        var secured = user.SecureWithVerifiedEmail();

        Assert.Multiple(() =>
        {
            Assert.That(secured, Is.True);
            Assert.That(user.HasPassword, Is.False);
            Assert.That(user.EmailVerified, Is.True);
            Assert.That(user.DomainEvents.OfType<UserPasswordRemovedDomainEvent>(), Has.Exactly(1).Items);
        });
    }

    [Test]
    public void SecureWithVerifiedEmail_ForAVerifiedAccount_KeepsThePassword()
    {
        var user = User.Create(new EmailAddress("ada@example.com"), new PersonName("Ada"), emailVerified: true, Now);
        user.SetPassword(new PasswordHash("hash"));

        Assert.Multiple(() =>
        {
            Assert.That(user.SecureWithVerifiedEmail(), Is.False);
            Assert.That(user.HasPassword, Is.True);
        });
    }

    [Test]
    public void UpgradePasswordHash_OnlyReplacesAnExistingPassword()
    {
        var withoutPassword = User.Create(new EmailAddress("ada@example.com"), new PersonName("Ada"), true, Now);
        var withPassword = Register();

        withoutPassword.UpgradePasswordHash(new PasswordHash("new"));
        withPassword.UpgradePasswordHash(new PasswordHash("new"));

        Assert.Multiple(() =>
        {
            Assert.That(withoutPassword.HasPassword, Is.False);
            Assert.That(withPassword.PasswordHash, Is.EqualTo(new PasswordHash("new")));
        });
    }

    [TestCase("short")]
    [TestCase("            ")]
    [TestCase(null)]
    public void Password_RejectsPasswordsOutsideThePolicy(string? value)
        => Assert.That(() => new Password(value), Throws.TypeOf<WeakPasswordException>());

    [Test]
    public void Password_RejectsTooLongPasswords()
        => Assert.That(() => new Password(new string('a', Password.MaxLength + 1)), Throws.TypeOf<WeakPasswordException>());

    [Test]
    public void Password_NeverShowsItsValue()
        => Assert.That(new Password("correct horse battery").ToString(), Does.Not.Contain("horse"));

    private static User Register()
        => User.RegisterWithPassword(new EmailAddress("ada@example.com"), new PersonName("Ada"), new PasswordHash("hash"), Now);
}
