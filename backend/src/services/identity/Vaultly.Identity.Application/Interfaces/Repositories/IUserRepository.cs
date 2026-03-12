using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Interfaces.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user by a linked external identity.
    /// </summary>
    Task<User?> GetByExternalIdentityAsync(ProviderId providerId, ProviderUserId providerUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user by their identifier.
    /// </summary>
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new user aggregate to the repository.
    /// </summary>
    void Add(User user);
}