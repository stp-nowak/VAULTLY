using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.Infrastructure.Persistence;
using Vaultly.Identity.Infrastructure.Persistence.ReadServices;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class SessionReadServiceTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task GetForUserAsync_WhenUserHasSessions_ReturnsOnlyOwnSessionsOrderedByLastUse()
    {
        await using var dbContext = CreateDbContext();
        var userId = UserId.New();
        var older = Session.Create(userId, new AuthMethod("google"), new UserAgent("Firefox"), new IpAddress("10.0.0.1"), Now);
        var newer = Session.Create(userId, new AuthMethod("google"), null, null, Now);
        newer.MarkUsed(Now.AddHours(1));
        var foreign = Session.Create(UserId.New(), new AuthMethod("google"), null, null, Now);
        dbContext.Sessions.AddRange(older, newer, foreign);
        await dbContext.SaveChangesAsync();

        var sut = new SessionReadService(dbContext);

        var result = await sut.GetForUserAsync(userId.Value, older.Id.Value, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { newer.Id.Value, older.Id.Value }));
            Assert.That(result[1].IsCurrent, Is.True);
            Assert.That(result[0].IsCurrent, Is.False);
            Assert.That(result[1].UserAgent, Is.EqualTo("Firefox"));
            Assert.That(result[1].IpAddress, Is.EqualTo("10.0.0.1"));
            Assert.That(result[0].UserAgent, Is.Null);
        });
    }

    [Test]
    public async Task GetForUserAsync_WhenCalled_DoesNotTrackReturnedSessions()
    {
        await using var dbContext = CreateDbContext();
        var userId = UserId.New();
        dbContext.Sessions.Add(Session.Create(userId, new AuthMethod("google"), null, null, Now));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var sut = new SessionReadService(dbContext);

        await sut.GetForUserAsync(userId.Value, Guid.NewGuid(), CancellationToken.None);

        Assert.That(dbContext.ChangeTracker.Entries<Session>(), Is.Empty);
    }

    /// <summary>
    /// Creates an isolated in-memory database context for read-side tests.
    /// </summary>
    private static IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new IdentityDbContext(options);
    }
}
