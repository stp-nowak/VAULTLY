using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.Domain.Users.ExternalIdentities;

namespace Vaultly.Identity.Infrastructure.Users;

internal sealed class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> entity)
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
    }
}
