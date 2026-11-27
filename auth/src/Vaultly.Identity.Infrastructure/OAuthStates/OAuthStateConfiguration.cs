using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.OAuthStates;

namespace Vaultly.Identity.Infrastructure.OAuthStates;

internal sealed class OAuthStateConfiguration : IEntityTypeConfiguration<OAuthState>
{
    public void Configure(EntityTypeBuilder<OAuthState> entity)
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
        entity.Property(x => x.AuthorizationRequestId)
            .HasConversion(id => id.Value, value => AuthorizationRequestId.From(value));
        entity.HasIndex(x => x.State).IsUnique();
    }
}
