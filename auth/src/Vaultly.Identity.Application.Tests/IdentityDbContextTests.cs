using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Core;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.AuthorizationRequests.Clients;
using Vaultly.Identity.Domain.OAuthStates;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Sessions.AuthCodes;
using Vaultly.Identity.Domain.Sessions.RefreshTokens;
using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.Infrastructure.Persistence;

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
            Assert.That(GetMaxLength<AuthorizationRequest>(dbContext, nameof(AuthorizationRequest.RedirectUri)), Is.EqualTo(RedirectUri.MaxLength));
            Assert.That(GetMaxLength<AuthorizationRequest>(dbContext, nameof(AuthorizationRequest.State)), Is.EqualTo(ClientState.MaxLength));
            Assert.That(GetMaxLength<AuthorizationRequest>(dbContext, nameof(AuthorizationRequest.ClientId)), Is.EqualTo(ClientId.MaxLength));
            Assert.That(GetMaxLength<AuthCode>(dbContext, nameof(AuthCode.CodeChallenge)), Is.EqualTo(CodeChallenge.Length));
            Assert.That(GetMaxLength<Session>(dbContext, nameof(Session.SsoTokenHash)), Is.EqualTo(TokenHash.MaxLength));
        });
    }

    [Test]
    public void Migrations_WhenComparedWithTheModel_LeaveNoPendingChanges()
    {
        using var dbContext = new IdentityDbContextDesignTimeFactory().CreateDbContext([]);

        Assert.That(
            dbContext.Database.HasPendingModelChanges(),
            Is.False,
            "The model changed without a migration: run `dotnet ef migrations add <Name>` (see auth/AGENTS.md).");
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

