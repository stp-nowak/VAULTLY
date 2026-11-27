using Microsoft.EntityFrameworkCore;


using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Infrastructure.Persistence;

namespace Vaultly.Identity.Infrastructure.AuthorizationRequests;

/// <summary>
/// Repository for authorization request aggregates backed by the identity DbContext.
/// </summary>
public sealed class AuthorizationRequestRepository(IdentityDbContext dbContext) : IAuthorizationRequestRepository
{
    private readonly IdentityDbContext _dbContext = dbContext;

    /// <summary>
    /// Adds a new authorization request to the change tracker.
    /// </summary>
    public void Add(AuthorizationRequest request) => _dbContext.AuthorizationRequests.Add(request);

    /// <summary>
    /// Gets an authorization request by id.
    /// </summary>
    public Task<AuthorizationRequest?> GetByIdAsync(AuthorizationRequestId id, CancellationToken cancellationToken)
        => _dbContext.AuthorizationRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
