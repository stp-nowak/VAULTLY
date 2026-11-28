using System.Globalization;

using NUnit.Framework;

using Vaultly.Core.Amounts;

namespace Vaultly.Core.Tests;

[TestFixture]
public sealed class AmountsTests
{
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
