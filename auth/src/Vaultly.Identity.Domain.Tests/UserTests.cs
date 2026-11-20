using NUnit.Framework;

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class UserTests
{
    [Test]
    public void Create_RaisesUserRegisteredDomainEvent()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var user = User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), true, now);

        Assert.That(user.DomainEvents.OfType<UserRegisteredDomainEvent>().Single().UserId, Is.EqualTo(user.Id));
    }

    [Test]
    public void LinkExternalIdentity_WhenDuplicate_ThrowsDuplicateExternalIdentityException()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var user = User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), true, now);
        var providerId = new ProviderId("google");
        var providerUserId = new ProviderUserId("subject-1");
        var email = new EmailAddress("user@example.com");
        var name = new PersonName("Test User");

        user.LinkExternalIdentity(providerId, providerUserId, email, name, now);

        Assert.That(
            () => user.LinkExternalIdentity(providerId, providerUserId, email, name, now),
            Throws.TypeOf<DuplicateExternalIdentityException>());
    }

    [Test]
    public void LinkExternalIdentity_WhenNew_RaisesExternalIdentityLinkedDomainEvent()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var user = User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), true, now);
        user.ClearDomainEvents();

        var providerId = new ProviderId("google");
        var providerUserId = new ProviderUserId("subject-1");
        user.LinkExternalIdentity(providerId, providerUserId, new EmailAddress("user@example.com"), new PersonName("Test User"), now);

        var domainEvent = user.DomainEvents.OfType<ExternalIdentityLinkedDomainEvent>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(domainEvent.UserId, Is.EqualTo(user.Id));
            Assert.That(domainEvent.ProviderId, Is.EqualTo(providerId));
            Assert.That(domainEvent.ProviderUserId, Is.EqualTo(providerUserId));
        });
    }

    [Test]
    public void UpdateProfile_WhenChanged_RaisesUserProfileUpdatedDomainEvent()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var user = User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), false, now);
        user.ClearDomainEvents();

        user.UpdateProfile(new PersonName("Updated User"), true);

        var domainEvent = user.DomainEvents.OfType<UserProfileUpdatedDomainEvent>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(domainEvent.UserId, Is.EqualTo(user.Id));
            Assert.That(domainEvent.EmailVerified, Is.True);
            Assert.That(domainEvent.Name, Is.EqualTo(new PersonName("Updated User")));
        });
    }
}