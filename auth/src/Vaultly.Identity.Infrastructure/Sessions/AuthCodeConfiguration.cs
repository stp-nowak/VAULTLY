using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Identity.Domain.AuthorizationRequests.Clients;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Sessions.AuthCodes;

namespace Vaultly.Identity.Infrastructure.Sessions;

internal sealed class AuthCodeConfiguration : IEntityTypeConfiguration<AuthCode>
{
    public void Configure(EntityTypeBuilder<AuthCode> entity)
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
        entity.Property(x => x.ClientId)
            .HasConversion(id => id.Value, value => new ClientId(value))
            .HasMaxLength(ClientId.MaxLength)
            .IsRequired();
        entity.Property(x => x.RedirectUri)
            .HasConversion(uri => uri.Value, value => new RedirectUri(value))
            .HasMaxLength(RedirectUri.MaxLength)
            .IsRequired();
        entity.Property(x => x.CodeChallenge)
            .HasConversion(challenge => challenge.Value, value => new CodeChallenge(value))
            .HasMaxLength(CodeChallenge.Length)
            .IsRequired();
        entity.HasIndex(x => x.CodeHash).IsUnique();
        entity.HasIndex(x => x.SessionId);
    }
}
