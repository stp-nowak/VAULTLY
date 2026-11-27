using NUnit.Framework;

namespace Vaultly.Core.Persistence.Tests;

[TestFixture]
public sealed class WireNamesTests
{
    public enum Kind
    {
        Simple,
        CreditCard
    }

    [TestCase(Kind.Simple, "simple")]
    [TestCase(Kind.CreditCard, "credit_card")]
    public void Of_SpellsTheMemberInSnakeCase(Kind kind, string expected)
        => Assert.That(WireNames.Of(kind), Is.EqualTo(expected));

    [TestCase("credit_card", true)]
    [TestCase("CreditCard", false)]
    [TestCase("1", false)]
    [TestCase(null, false)]
    public void TryParse_AcceptsOnlyTheWireSpelling(string? value, bool expected)
        => Assert.That(WireNames.TryParse<Kind>(value, out _), Is.EqualTo(expected));

    [Test]
    public void Converter_RoundTripsThroughTheWireSpelling()
    {
        var converter = new WireNameConverter<Kind>();

        Assert.Multiple(() =>
        {
            Assert.That(converter.ConvertToProvider(Kind.CreditCard), Is.EqualTo("credit_card"));
            Assert.That(converter.ConvertFromProvider("credit_card"), Is.EqualTo(Kind.CreditCard));
        });
    }
}
