namespace Vaultly.Accounts.PublicTypes.Events;

/// <summary>A user opened an account; amounts on it are in <paramref name="Currency"/> (ISO 4217).</summary>
/// <param name="Version">The account's version after the change; a copy kept elsewhere ignores older versions.</param>
public sealed record AccountOpened(Guid AccountId, Guid OwnerId, string Currency, int Version);
