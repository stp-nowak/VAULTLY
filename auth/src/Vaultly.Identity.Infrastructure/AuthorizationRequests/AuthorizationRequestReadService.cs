using Microsoft.EntityFrameworkCore;

using Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Infrastructure.Persistence;

namespace Vaultly.Identity.Infrastructure.AuthorizationRequests;

/// <summary>
/// EF Core read side for authorization requests (no tracking, projection without secrets).
/// </summary>
public sealed class AuthorizationRequestReadService(IdentityDbContext dbContext) : IAuthorizationRequestReadService
{
    private readonly IdentityDbContext _dbContext = dbContext;

    /// <summary>
    /// Gets the login-UI view of an authorization request.
    /// </summary>
    public Task<AuthorizationRequestReadModel?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var requestId = AuthorizationRequestId.From(id);
        return _dbContext.AuthorizationRequests
            .AsNoTracking()
            .Where(x => x.Id == requestId)
            .Select(x => new AuthorizationRequestReadModel(x.Id.Value, x.ClientId.Value, x.ExpiresAt, x.CompletedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
