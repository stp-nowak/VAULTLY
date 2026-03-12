using Microsoft.EntityFrameworkCore;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core repository for OAuth state aggregates.
/// </summary>
public sealed class EfOAuthStateRepository(IdentityDbContext dbContext) : IOAuthStateRepository
{
    private readonly IdentityDbContext _dbContext = dbContext;

    /// <summary>
    /// Gets an OAuth state aggregate by its state value.
    /// </summary>
    public Task<OAuthState?> GetByStateAsync(OAuthStateValue state, CancellationToken cancellationToken)
        => _dbContext.OAuthStates.SingleOrDefaultAsync(x => x.State == state, cancellationToken);

    /// <summary>
    /// Adds a new OAuth state aggregate to the EF Core change tracker.
    /// </summary>
    public void Add(OAuthState state) => _dbContext.OAuthStates.Add(state);

    /// <summary>
    /// Removes an OAuth state aggregate from the EF Core change tracker.
    /// </summary>
    public void Remove(OAuthState state) => _dbContext.OAuthStates.Remove(state);
}