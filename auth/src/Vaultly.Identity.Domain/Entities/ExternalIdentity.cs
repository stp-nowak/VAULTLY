using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Entities;

public sealed class ExternalIdentity
{
    /// <summary>
    /// Required by EF Core.
    /// </summary>
    private ExternalIdentity()
    {
    }

    public ExternalIdentityId Id { get; private set; }
    public UserId UserId { get; private set; }
    public ProviderId ProviderId { get; private set; } = null!;
    public ProviderUserId ProviderUserId { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public PersonName Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new external identity entity.
    /// </summary>
    internal static ExternalIdentity Create(
        UserId userId,
        ProviderId providerId,
        ProviderUserId providerUserId,
        EmailAddress email,
        PersonName name,
        DateTimeOffset now)
    {
        return new ExternalIdentity
        {
            Id = ExternalIdentityId.New(),
            UserId = userId,
            ProviderId = providerId,
            ProviderUserId = providerUserId,
            Email = email,
            Name = name,
            CreatedAt = now
        };
    }
}