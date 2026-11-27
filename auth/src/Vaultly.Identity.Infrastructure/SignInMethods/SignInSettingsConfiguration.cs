using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Identity.Domain.SignInMethods;

namespace Vaultly.Identity.Infrastructure.SignInMethods;

internal sealed class SignInSettingsConfiguration : IEntityTypeConfiguration<SignInSettings>
{
    public void Configure(EntityTypeBuilder<SignInSettings> entity)
    {
        entity.Ignore(x => x.DomainEvents);
        entity.Ignore(x => x.DisabledMethods);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.PrimitiveCollection<List<string>>("DisabledMethodValues").HasColumnName("DisabledMethods");
    }
}
