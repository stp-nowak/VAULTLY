using Vaultly.Core;
using Vaultly.Core.Amounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Domain.Accounts;

/// <summary>
/// What Transactions knows of an account of the Accounts service, kept from its events: whose it is, its currency and
/// whether it is closed. Events can arrive late or twice, so one older than what is kept changes nothing.
/// </summary>
public sealed class Account : AggregateRoot
{
    private Account()
    {
    }

    public AccountId Id { get; private set; }

    public OwnerId OwnerId { get; private set; }

    /// <summary><c>null</c> only when the account's closing arrived before its opening.</summary>
    public Currency? Currency { get; private set; }

    public bool IsClosed { get; private set; }

    /// <summary>The account's version in the Accounts service as of the last applied event.</summary>
    public int Version { get; private set; }

    public static Account Opened(AccountId id, OwnerId ownerId, Currency currency, int version)
        => new() { Id = id, OwnerId = ownerId, Currency = currency, Version = version };

    public static Account Closed(AccountId id, OwnerId ownerId, int version)
        => new() { Id = id, OwnerId = ownerId, IsClosed = true, Version = version };

    public void ApplyOpened(Currency currency, int version)
    {
        Currency ??= currency;
        Version = Math.Max(Version, version);
    }

    public void ApplyClosed(int version)
    {
        if (version > Version)
        {
            IsClosed = true;
            Version = version;
        }
    }
}
