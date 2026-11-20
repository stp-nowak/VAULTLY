using NUnit.Framework;

using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class ValueObjectLengthTests
{
    [Test]
    public void EmailAddress_WhenValueExceedsMaxLength_ThrowsInvalidValueException()
    {
        var localPart = new string('a', EmailAddress.MaxLength - "@x.com".Length + 1);
        var email = $"{localPart}@x.com";

        Assert.That(() => new EmailAddress(email), Throws.TypeOf<InvalidValueException>());
    }

    [Test]
    public void EmailAddress_WhenValueMatchesMaxLength_CreatesValueObject()
    {
        var localPart = new string('a', EmailAddress.MaxLength - "@x.com".Length);
        var email = $"{localPart}@x.com";

        var valueObject = new EmailAddress(email);

        Assert.That(valueObject.Value, Is.EqualTo(email.ToLowerInvariant()));
    }

    [Test]
    public void RedirectUri_WhenValueExceedsMaxLength_ThrowsInvalidValueException()
    {
        var prefix = "https://example.com/";
        var path = new string('a', RedirectUri.MaxLength - prefix.Length + 1);
        var uri = $"{prefix}{path}";

        Assert.That(() => new RedirectUri(uri), Throws.TypeOf<InvalidValueException>());
    }

    [Test]
    public void RedirectUri_WhenValueMatchesMaxLength_CreatesValueObject()
    {
        var prefix = "https://example.com/";
        var path = new string('a', RedirectUri.MaxLength - prefix.Length);
        var uri = $"{prefix}{path}";

        var valueObject = new RedirectUri(uri);

        Assert.That(valueObject.Value, Is.EqualTo(uri));
    }
}

