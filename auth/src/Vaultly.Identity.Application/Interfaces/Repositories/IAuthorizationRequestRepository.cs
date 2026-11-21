using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Interfaces.Repositories;

public interface IAuthorizationRequestRepository
{
    /// <summary>
    /// Adds a new authorization request.
    /// </summary>
    void Add(AuthorizationRequest request);

    /// <summary>
    /// Gets an authorization request by id.
    /// </summary>
    Task<AuthorizationRequest?> GetByIdAsync(AuthorizationRequestId id, CancellationToken cancellationToken);
}
