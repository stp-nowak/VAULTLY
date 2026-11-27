namespace Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;

/// <summary>
/// Read side (CQRS) for authorization requests shown by the login UI.
/// </summary>
public interface IAuthorizationRequestReadService
{
    /// <summary>
    /// Gets the minimal view of an authorization request, or <c>null</c> when it does not exist.
    /// </summary>
    Task<AuthorizationRequestReadModel?> GetAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>
/// Read model of an authorization request (no secrets: no state, challenge or redirect URI).
/// </summary>
public sealed record AuthorizationRequestReadModel(Guid Id, string ClientId, DateTimeOffset ExpiresAt, DateTimeOffset? CompletedAt);
