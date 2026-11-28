using Vaultly.Accounts.Domain.Accounts.Events;
using Vaultly.Core;
using Vaultly.Core.Amounts;

namespace Vaultly.Accounts.Domain.Accounts;

/// <summary>
/// A financial account of one user in one currency. Its balance starts at the opening balance; later it changes
/// only through the user's transactions. A closed account cannot be changed.
/// </summary>
public sealed class Account : AggregateRoot
{
    private Account()
    {
    }

    public AccountId Id { get; private set; }

    public OwnerId OwnerId { get; private set; }

    public AccountName Name { get; private set; } = null!;

    public AccountType Type { get; private set; }

    public Currency Currency { get; private set; } = null!;

    public decimal OpeningBalance { get; private set; }

    public decimal Balance { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public bool IsClosed => ClosedAt is not null;

    public static Account Open(OwnerId ownerId, AccountName name, AccountType type, Money openingBalance, DateTimeOffset now)
    {
        if (!Enum.IsDefined(type))
        {
            throw new InvalidValueException("Unknown account type.");
        }

        var account = new Account
        {
            Id = AccountId.New(),
            OwnerId = ownerId,
            Name = name,
            Type = type,
            Currency = openingBalance.Currency,
            OpeningBalance = openingBalance.Amount,
            Balance = openingBalance.Amount,
            OpenedAt = now
        };

        account.RaiseDomainEvent(new AccountOpened(account.Id, ownerId, type, account.Currency));
        return account;
    }

    public void Rename(AccountName name)
    {
        EnsureOpen();
        Name = name;
    }

    public void Close(DateTimeOffset now)
    {
        EnsureOpen();
        ClosedAt = now;
        RaiseDomainEvent(new AccountClosed(Id, OwnerId));
    }

    private void EnsureOpen()
    {
        if (IsClosed)
        {
            throw new AccountClosedException(Id);
        }
    }
}
