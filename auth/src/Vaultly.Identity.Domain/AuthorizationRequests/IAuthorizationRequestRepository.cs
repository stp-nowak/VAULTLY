namespace Vaultly.Identity.Domain.AuthorizationRequests;

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
