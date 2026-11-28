namespace Vaultly.Accounts.PublicTypes.Events;

/// <summary>A user closed an account; nothing may be booked on it any more.</summary>
/// <param name="Version">The account's version after the change; a copy kept elsewhere ignores older versions.</param>
public sealed record AccountClosed(Guid AccountId, Guid OwnerId, int Version);
