using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Entities;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Domain events are dispatched through the unit of work and must never be mapped as persisted entities.
        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.Entity<User>(entity =>
        {
            entity.Ignore(x => x.DomainEvents);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .HasConversion(id => id.Value, value => UserId.From(value));
            entity.Property(x => x.Email)
                .HasConversion(email => email.Value, value => new EmailAddress(value))
                .HasMaxLength(EmailAddress.MaxLength)
                .IsRequired();
            entity.Property(x => x.Name)
                .HasConversion(name => name.Value, value => new PersonName(value))
                .HasMaxLength(PersonName.MaxLength)
                .IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasMany(x => x.ExternalIdentities)
                .WithOne()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.ExternalIdentities).HasField("_externalIdentities");
        });

        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .HasConversion(id => id.Value, value => ExternalIdentityId.From(value));
            entity.Property(x => x.UserId)
                .HasConversion(id => id.Value, value => UserId.From(value));
            entity.Property(x => x.ProviderId)
                .HasConversion(id => id.Value, value => new ProviderId(value))
                .HasMaxLength(ProviderId.MaxLength)
                .IsRequired();
            entity.Property(x => x.ProviderUserId)
                .HasConversion(id => id.Value, value => new ProviderUserId(value))
                .HasMaxLength(ProviderUserId.MaxLength)
                .IsRequired();
            entity.Property(x => x.Email)
                .HasConversion(email => email.Value, value => new EmailAddress(value))
                .HasMaxLength(EmailAddress.MaxLength)
                .IsRequired();
            entity.Property(x => x.Name)
                .HasConversion(name => name.Value, value => new PersonName(value))
                .HasMaxLength(PersonName.MaxLength)
                .IsRequired();
            entity.HasIndex(x => new { x.ProviderId, x.ProviderUserId }).IsUnique();
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.Ignore(x => x.DomainEvents);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .HasConversion(id => id.Value, value => SessionId.From(value));
            entity.Property(x => x.UserId)
                .HasConversion(id => id.Value, value => UserId.From(value));
            entity.Property(x => x.AuthMethod)
                .HasConversion(method => method.Value, value => new AuthMethod(value))
                .HasMaxLength(AuthMethod.MaxLength)
                .IsRequired();
            entity.Property(x => x.UserAgent)
                .HasConversion(
                    value => value == null ? null : value.Value,
                    value => value == null ? null : new UserAgent(value))
                .HasMaxLength(UserAgent.MaxLength);
            entity.Property(x => x.IpAddress)
                .HasConversion(
                    value => value == null ? null : value.Value,
                    value => value == null ? null : new IpAddress(value))
                .HasMaxLength(IpAddress.MaxLength);
            entity.HasIndex(x => x.UserId);
            entity.HasMany(x => x.RefreshTokens)
                .WithOne()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.RefreshTokens).HasField("_refreshTokens");
            entity.HasMany(x => x.AuthCodes)
                .WithOne()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.AuthCodes).HasField("_authCodes");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .HasConversion(id => id.Value, value => RefreshTokenId.From(value));
            entity.Property(x => x.SessionId)
                .HasConversion(id => id.Value, value => SessionId.From(value));
            entity.Property(x => x.TokenHash)
                .HasConversion(hash => hash.Value, value => new TokenHash(value))
                .HasMaxLength(TokenHash.MaxLength)
                .IsRequired();
            // Explicit ValueConverter required to disambiguate nullable HasConversion overloads in EF Core 10.
            // id.Value.Value: outer .Value unwraps Nullable<RefreshTokenId>, inner .Value is the Guid.
            entity.Property(x => x.ReplacedByTokenId)
                .HasConversion(
                    new ValueConverter<RefreshTokenId?, Guid?>(
                        id => id == null ? null : id.Value.Value,
                        value => value == null ? null : RefreshTokenId.From(value.Value)));
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.SessionId);
        });

        modelBuilder.Entity<AuthCode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .HasConversion(id => id.Value, value => AuthCodeId.From(value));
            entity.Property(x => x.SessionId)
                .HasConversion(id => id.Value, value => SessionId.From(value));
            entity.Property(x => x.CodeHash)
                .HasConversion(hash => hash.Value, value => new CodeHash(value))
                .HasMaxLength(CodeHash.MaxLength)
                .IsRequired();
            entity.HasIndex(x => x.CodeHash).IsUnique();
            entity.HasIndex(x => x.SessionId);
        });

        modelBuilder.Entity<OAuthState>(entity =>
        {
            entity.Ignore(x => x.DomainEvents);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .HasConversion(id => id.Value, value => OAuthStateId.From(value));
            entity.Property(x => x.State)
                .HasConversion(state => state.Value, value => new OAuthStateValue(value))
                .HasMaxLength(OAuthStateValue.MaxLength)
                .IsRequired();
            entity.Property(x => x.CodeVerifier)
                .HasConversion(verifier => verifier.Value, value => new CodeVerifier(value))
                .HasMaxLength(CodeVerifier.MaxLength)
                .IsRequired();
            entity.Property(x => x.Nonce)
                .HasConversion(nonce => nonce.Value, value => new Nonce(value))
                .HasMaxLength(Nonce.MaxLength)
                .IsRequired();
            entity.Property(x => x.RedirectUri)
                .HasConversion(
                    url => url == null ? null : url.Value,
                    value => value == null ? null : new RedirectUri(value))
                .HasMaxLength(RedirectUri.MaxLength);
            entity.HasIndex(x => x.State).IsUnique();
        });
    }
}