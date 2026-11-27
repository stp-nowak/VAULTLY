using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.AuthorizationRequests.Clients;

namespace Vaultly.Identity.Infrastructure.AuthorizationRequests;

internal sealed class AuthorizationRequestConfiguration : IEntityTypeConfiguration<AuthorizationRequest>
{
    public void Configure(EntityTypeBuilder<AuthorizationRequest> entity)
    {
        entity.Ignore(x => x.DomainEvents);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AuthorizationRequestId.From(value));
        entity.Property(x => x.ClientId)
            .HasConversion(id => id.Value, value => new ClientId(value))
            .HasMaxLength(ClientId.MaxLength)
            .IsRequired();
        entity.Property(x => x.RedirectUri)
            .HasConversion(uri => uri.Value, value => new RedirectUri(value))
            .HasMaxLength(RedirectUri.MaxLength)
            .IsRequired();
        // Both are null for an admin console sign-in, which is bound to no client.
        entity.Property(x => x.State)
            .HasConversion(state => state!.Value, value => new ClientState(value))
            .HasMaxLength(ClientState.MaxLength);
        entity.Property(x => x.CodeChallenge)
            .HasConversion(challenge => challenge!.Value, value => new CodeChallenge(value))
            .HasMaxLength(CodeChallenge.Length);
        entity.Ignore(x => x.IsCompleted);
        entity.Ignore(x => x.IsForConsole);
        entity.HasIndex(x => x.ExpiresAt);
    }
}
