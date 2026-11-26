using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Services;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.Infrastructure.Persistence;
using Vaultly.Identity.Infrastructure.Persistence.ReadServices;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class UserReadServiceTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task GetPageAsync_ReturnsNewestUsersFirstWithTotalAndSignInMethods()
    {
        await using var dbContext = CreateDbContext();
        var oldest = AddUser(dbContext, "ada@example.com", "Ada Lovelace", Now.AddDays(-3));
        oldest.LinkExternalIdentity(new ProviderId("google"), new ProviderUserId("g-1"), oldest.Email, oldest.Name, Now);
        var middle = AddUser(dbContext, "grace@example.com", "Grace Hopper", Now.AddDays(-2));
        RoleAssignment.Assign(middle, UserRole.Admin, adminCount: 0);
        AddUser(dbContext, "alan@example.com", "Alan Turing", Now.AddDays(-1));
        await dbContext.SaveChangesAsync();

        var page = await new UserReadService(dbContext).GetPageAsync(null, page: 2, pageSize: 1, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(page.Total, Is.EqualTo(3));
            Assert.That(page.Page, Is.EqualTo(2));
            Assert.That(page.Items.Single().Email, Is.EqualTo("grace@example.com"));
            Assert.That(page.Items.Single().Role, Is.EqualTo("admin"));
        });

        var last = await new UserReadService(dbContext).GetPageAsync(null, page: 3, pageSize: 1, CancellationToken.None);
        Assert.That(last.Items.Single().SignInMethods, Is.EqualTo(new[] { "google" }));
    }

    [TestCase("GRACE", "grace@example.com")]
    [TestCase("turing", "alan@example.com")]
    public async Task GetPageAsync_WithSearch_MatchesEmailOrNameIgnoringCase(string search, string expectedEmail)
    {
        await using var dbContext = CreateDbContext();
        AddUser(dbContext, "grace@example.com", "Grace Hopper", Now.AddDays(-2));
        AddUser(dbContext, "alan@example.com", "Alan Turing", Now.AddDays(-1));
        await dbContext.SaveChangesAsync();

        var page = await new UserReadService(dbContext).GetPageAsync(search, 1, 20, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(page.Total, Is.EqualTo(1));
            Assert.That(page.Items.Single().Email, Is.EqualTo(expectedEmail));
        });
    }

    [Test]
    public void GetPageAsync_WithSearch_FiltersInTheDatabase()
    {
        using var dbContext = new IdentityDbContextDesignTimeFactory().CreateDbContext([]);

        var sql = UserReadService.Search(dbContext.Users, "Ada").ToQueryString();

        Assert.That(sql, Does.Contain("lower(u.\"Email\"::text) LIKE").And.Contain("lower(u.\"Name\"::text) LIKE"));
    }

    [Test]
    public async Task GetBySsoTokenHashAsync_ReturnsUserOfActiveSessionOnly()
    {
        await using var dbContext = CreateDbContext();
        var user = AddUser(dbContext, "grace@example.com", "Grace Hopper", Now);
        dbContext.Sessions.Add(Session.Create(user.Id, new AuthMethod("google"), null, null, new TokenHash("active"), Now, Now.AddHours(1)));
        dbContext.Sessions.Add(Session.Create(user.Id, new AuthMethod("google"), null, null, new TokenHash("expired"), Now.AddHours(-2), Now.AddMinutes(-1)));
        var revoked = Session.Create(user.Id, new AuthMethod("google"), null, null, new TokenHash("revoked"), Now, Now.AddHours(1));
        revoked.Revoke(Now);
        dbContext.Sessions.Add(revoked);
        await dbContext.SaveChangesAsync();
        var sut = new UserReadService(dbContext);

        var active = await sut.GetBySsoTokenHashAsync("active", Now, CancellationToken.None);

        Assert.Multiple(async () =>
        {
            Assert.That(active, Is.EqualTo(new Application.Models.ConsoleUserDto(user.Id.Value, "grace@example.com", "Grace Hopper", "user")));
            Assert.That(await sut.GetBySsoTokenHashAsync("expired", Now, CancellationToken.None), Is.Null);
            Assert.That(await sut.GetBySsoTokenHashAsync("revoked", Now, CancellationToken.None), Is.Null);
            Assert.That(await sut.GetBySsoTokenHashAsync("unknown", Now, CancellationToken.None), Is.Null);
        });
    }

    private static User AddUser(IdentityDbContext dbContext, string email, string name, DateTimeOffset createdAt)
    {
        var user = User.Create(new EmailAddress(email), new PersonName(name), true, createdAt);
        dbContext.Users.Add(user);
        return user;
    }

    private static IdentityDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
}
