using Microsoft.EntityFrameworkCore;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Domain.Aggregates;

namespace Vaultly.Identity.Infrastructure.Persistence.Repositories;

public sealed class SignInSettingsRepository(IdentityDbContext dbContext) : ISignInSettingsRepository
{
    private readonly IdentityDbContext _dbContext = dbContext;

    public Task<SignInSettings?> FindAsync(CancellationToken cancellationToken)
        => _dbContext.SignInSettings.SingleOrDefaultAsync(x => x.Id == SignInSettings.SingletonId, cancellationToken);

    public void Add(SignInSettings settings) => _dbContext.SignInSettings.Add(settings);
}
