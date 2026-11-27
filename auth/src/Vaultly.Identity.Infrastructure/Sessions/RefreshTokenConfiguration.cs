using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Sessions.RefreshTokens;

namespace Vaultly.Identity.Infrastructure.Sessions;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
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
    }
}
