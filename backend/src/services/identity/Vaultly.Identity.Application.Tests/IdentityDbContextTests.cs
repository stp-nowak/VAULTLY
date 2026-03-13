using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Entities;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.Infrastructure.Persistence;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class IdentityDbContextTests
{
    [Test]
    public async Task SaveChangesAsync_WhenAggregateHasDomainEvents_DoesNotMapDomainEventAsEntity()
    {
        await using var dbContext = CreateDbContext();

        var user = User.Create(
            new EmailAddress("user@example.com"),
            new PersonName("Test User"),
            emailVerified: true,
            now: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        dbContext.Users.Add(user);

        Assert.Multiple(() =>
        {
            Assert.That(dbContext.Model.FindEntityType(typeof(DomainEvent)), Is.Null);
            Assert.That(dbContext.Model.FindEntityType(typeof(User)), Is.Not.Null);
        });

        Assert.DoesNotThrowAsync(async () => await dbContext.SaveChangesAsync());
    }

    [Test]
    public void Model_WhenStringBackedValueObjectsAreMapped_UsesSharedMaxLengthConstants()
    {
        using var dbContext = CreateDbContext();

        Assert.Multiple(() =>
        {
            Assert.That(GetMaxLength<User>(dbContext, nameof(User.Email)), Is.EqualTo(EmailAddress.MaxLength));
            Assert.That(GetMaxLength<User>(dbContext, nameof(User.Name)), Is.EqualTo(PersonName.MaxLength));
            Assert.That(GetMaxLength<Session>(dbContext, nameof(Session.AuthMethod)), Is.EqualTo(AuthMethod.MaxLength));
            Assert.That(GetMaxLength<RefreshToken>(dbContext, nameof(RefreshToken.TokenHash)), Is.EqualTo(TokenHash.MaxLength));
            Assert.That(GetMaxLength<AuthCode>(dbContext, nameof(AuthCode.CodeHash)), Is.EqualTo(CodeHash.MaxLength));
            Assert.That(GetMaxLength<OAuthState>(dbContext, nameof(OAuthState.State)), Is.EqualTo(OAuthStateValue.MaxLength));
            Assert.That(GetMaxLength<OAuthState>(dbContext, nameof(OAuthState.CodeVerifier)), Is.EqualTo(CodeVerifier.MaxLength));
            Assert.That(GetMaxLength<OAuthState>(dbContext, nameof(OAuthState.Nonce)), Is.EqualTo(Nonce.MaxLength));
            Assert.That(GetMaxLength<OAuthState>(dbContext, nameof(OAuthState.RedirectUri)), Is.EqualTo(RedirectUri.MaxLength));
        });
    }

    /// <summary>
    /// Creates an isolated in-memory database context for EF Core persistence tests.
    /// </summary>
    private static IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new IdentityDbContext(options);
    }

    /// <summary>
    /// Reads the configured EF Core maximum length for a mapped scalar property.
    /// </summary>
    private static int? GetMaxLength<TEntity>(IdentityDbContext dbContext, string propertyName)
        where TEntity : class
        => dbContext.Model.FindEntityType(typeof(TEntity))?.FindProperty(propertyName)?.GetMaxLength();
}

