using Microsoft.EntityFrameworkCore;

using Vaultly.Core;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.OAuthStates;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Sessions.AuthCodes;
using Vaultly.Identity.Domain.Sessions.RefreshTokens;
using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.Domain.Users.ExternalIdentities;

namespace Vaultly.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuthCode> AuthCodes => Set<AuthCode>();
    public DbSet<OAuthState> OAuthStates => Set<OAuthState>();
    public DbSet<AuthorizationRequest> AuthorizationRequests => Set<AuthorizationRequest>();
    public DbSet<SignInSettings> SignInSettings => Set<SignInSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Domain events are dispatched through the unit of work and must never be mapped as persisted entities.
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}