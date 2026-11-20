using NUnit.Framework;

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class OAuthStateTests
{
    [Test]
    public void Create_RaisesOAuthStateCreatedDomainEvent()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var state = OAuthState.Create(
            new OAuthStateValue("state"),
            new CodeVerifier("verifier"),
            new Nonce("nonce"),
            now.AddMinutes(5),
            null);

        Assert.That(state.DomainEvents.OfType<OAuthStateCreatedDomainEvent>().Single().OAuthStateId, Is.EqualTo(state.Id));
    }

    [Test]
    public void IsExpired_WhenNowAfterExpiry_ReturnsTrue()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expired = OAuthState.Create(
            new OAuthStateValue("state"),
            new CodeVerifier("verifier"),
            new Nonce("nonce"),
            now.AddMinutes(-1),
            null);

        Assert.That(expired.IsExpired(now), Is.True);
    }

    [Test]
    public void IsExpired_WhenNowBeforeExpiry_ReturnsFalse()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var active = OAuthState.Create(
            new OAuthStateValue("state"),
            new CodeVerifier("verifier"),
            new Nonce("nonce"),
            now.AddMinutes(5),
            null);

        Assert.That(active.IsExpired(now), Is.False);
    }

    [Test]
    public void EnsureActive_WhenExpired_ThrowsOAuthStateExpiredException()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expired = OAuthState.Create(
            new OAuthStateValue("state"),
            new CodeVerifier("verifier"),
            new Nonce("nonce"),
            now.AddMinutes(-1),
            null);

        Assert.That(() => expired.EnsureActive(now), Throws.TypeOf<OAuthStateExpiredException>());
    }

    [Test]
    public void Create_WhenRedirectUriProvided_PersistsRedirectUri()
    {
        var state = OAuthState.Create(
            new OAuthStateValue("state"),
            new CodeVerifier("verifier"),
            new Nonce("nonce"),
            new DateTimeOffset(2025, 1, 1, 0, 5, 0, TimeSpan.Zero),
            new RedirectUri("https://app.test/callback"));

        Assert.That(state.RedirectUri?.Value, Is.EqualTo("https://app.test/callback"));
    }
}