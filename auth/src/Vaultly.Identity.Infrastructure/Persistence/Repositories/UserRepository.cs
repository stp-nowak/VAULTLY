using Microsoft.EntityFrameworkCore;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for user aggregates backed by the identity DbContext.
/// </summary>
public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    private readonly IdentityDbContext _dbContext = dbContext;

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    public Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
        => _dbContext.Users
            .Include(x => x.ExternalIdentities)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    /// <summary>
    /// Gets a user by a linked external identity.
    /// </summary>
    public Task<User?> GetByExternalIdentityAsync(ProviderId providerId, ProviderUserId providerUserId, CancellationToken cancellationToken)
        => _dbContext.Users
            .Include(x => x.ExternalIdentities)
            .SingleOrDefaultAsync(
                x => x.ExternalIdentities.Any(e => e.ProviderId == providerId && e.ProviderUserId == providerUserId),
                cancellationToken);

    /// <summary>
    /// Gets a user by their identifier.
    /// </summary>
    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken)
        => _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    /// <summary>
    /// Adds a new user aggregate to the EF Core change tracker.
    /// </summary>
    public void Add(User user) => _dbContext.Users.Add(user);
}
