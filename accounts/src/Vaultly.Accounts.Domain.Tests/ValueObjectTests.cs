using NUnit.Framework;

using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Tests;

[TestFixture]
public sealed class ValueObjectTests
{
    [Test]
    public void AccountName_IsTrimmed()
        => Assert.That(new AccountName("  Wallet ").Value, Is.EqualTo("Wallet"));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AccountName_WithoutText_Throws(string? name)
        => Assert.Throws<InvalidValueException>(() => _ = new AccountName(name));

    [Test]
    public void AccountName_LongerThanTheLimit_Throws()
        => Assert.Throws<InvalidValueException>(() => _ = new AccountName(new string('a', AccountName.MaxLength + 1)));
}
