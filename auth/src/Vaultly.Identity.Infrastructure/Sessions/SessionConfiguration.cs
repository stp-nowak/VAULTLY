using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Infrastructure.Sessions;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> entity)
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
        entity.Property(x => x.SsoTokenHash)
            .HasConversion(hash => hash.Value, value => new TokenHash(value))
            .HasMaxLength(TokenHash.MaxLength)
            .IsRequired();
        entity.HasIndex(x => x.SsoTokenHash).IsUnique();
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
    }
}
