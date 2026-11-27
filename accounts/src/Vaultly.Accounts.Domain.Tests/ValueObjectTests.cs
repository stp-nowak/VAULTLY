using System.Globalization;

using NUnit.Framework;


using Vaultly.Accounts.Domain.Accounts;

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

    [TestCase("PLN", "PLN")]
    [TestCase(" eur ", "EUR")]
    [TestCase("usd", "USD")]
    public void Currency_AcceptsIsoCodes_InAnyCase(string code, string expected)
        => Assert.That(new Currency(code).Code, Is.EqualTo(expected));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("PL")]
    [TestCase("ABC")]
    [TestCase("PLNX")]
    public void Currency_RejectsAnythingElse(string? code)
        => Assert.Throws<InvalidValueException>(() => _ = new Currency(code));

    [TestCase("0")]
    [TestCase("-1234.5678")]
    [TestCase("999999999999999.9999")]
    public void Money_AcceptsSignedAmounts_WithUpToFourDecimals(string amount)
        => Assert.That(new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), new Currency("PLN")).Amount, Is.EqualTo(decimal.Parse(amount, CultureInfo.InvariantCulture)));

    [TestCase("0.00001")]
    [TestCase("1000000000000000")]
    [TestCase("-1000000000000000")]
    public void Money_RejectsMorePrecisionOrRangeThanStored(string amount)
        => Assert.Throws<InvalidValueException>(() => _ = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), new Currency("PLN")));
}
