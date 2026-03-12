using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Interfaces.Repositories;

public interface IOAuthStateRepository
{
    /// <summary>
    /// Gets an OAuth state aggregate by its state value.
    /// </summary>
    Task<OAuthState?> GetByStateAsync(OAuthStateValue state, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new OAuth state aggregate.
    /// </summary>
    void Add(OAuthState state);

    /// <summary>
    /// Removes an OAuth state aggregate.
    /// </summary>
    void Remove(OAuthState state);
}