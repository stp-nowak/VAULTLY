using NUnit.Framework;

using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Sessions.Events;
using Vaultly.Identity.Domain.Sessions.RefreshTokens.Events;
using Vaultly.Identity.Domain.Sessions.RefreshTokens.Exceptions;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class SessionTests
{
    [Test]
    public void Create_RaisesSessionStartedDomainEvent()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var session = Session.Create(
            UserId.New(),
            new AuthMethod("password"),
            UserAgent.FromNullable("unit-test"),
            IpAddress.FromNullable("127.0.0.1"),
            new TokenHash("sso-token"),
            now,
            now.AddHours(12));

        Assert.That(session.DomainEvents.OfType<SessionStarted>().Single().SessionId, Is.EqualTo(session.Id));
    }

    [Test]
    public void RotateRefreshToken_WhenMissingExistingToken_ThrowsRefreshTokenNotFoundException()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var session = Session.Create(
            UserId.New(),
            new AuthMethod("password"),
            UserAgent.FromNullable("unit-test"),
            IpAddress.FromNullable("127.0.0.1"),
            new TokenHash("sso-token"),
            now,
            now.AddHours(12));

        var existingHash = new TokenHash("missing-token");
        var newHash = new TokenHash("new-token");

        Assert.That(
            () => session.RotateRefreshToken(existingHash, newHash, now, now.AddDays(1)),
            Throws.TypeOf<RefreshTokenNotFoundException>());
    }

    [Test]
    public void RevokeWithTokens_WhenActive_RaisesSessionRevokedDomainEvent()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var session = Session.Create(
            UserId.New(),
            new AuthMethod("password"),
            UserAgent.FromNullable("unit-test"),
            IpAddress.FromNullable("127.0.0.1"),
            new TokenHash("sso-token"),
            now,
            now.AddHours(12));
        session.ClearDomainEvents();

        session.RevokeWithTokens(now.AddMinutes(5));

        var domainEvent = session.DomainEvents.OfType<SessionRevoked>().Single();
        Assert.That(domainEvent.SessionId, Is.EqualTo(session.Id));
    }

    [Test]
    public void RotateRefreshToken_WhenValid_RaisesRefreshTokenRotatedDomainEvent()
    {
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var session = Session.Create(
            UserId.New(),
            new AuthMethod("password"),
            UserAgent.FromNullable("unit-test"),
            IpAddress.FromNullable("127.0.0.1"),
            new TokenHash("sso-token"),
            now,
            now.AddHours(12));
        session.ClearDomainEvents();

        var existing = session.IssueRefreshToken(new TokenHash("existing-token"), now, now.AddDays(7));
        session.ClearDomainEvents();

        var rotated = session.RotateRefreshToken(existing.TokenHash, new TokenHash("new-token"), now.AddMinutes(1), now.AddDays(8));

        var domainEvent = session.DomainEvents.OfType<RefreshTokenRotated>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(domainEvent.SessionId, Is.EqualTo(session.Id));
            Assert.That(domainEvent.PreviousRefreshTokenId, Is.EqualTo(existing.Id));
            Assert.That(domainEvent.RefreshTokenId, Is.EqualTo(rotated.Id));
        });
    }
}