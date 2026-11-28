using Microsoft.EntityFrameworkCore;

using Vaultly.Transactions.Application.Features.Transactions;
using Vaultly.Transactions.Application.Features.Transactions.GetTransactions;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;
using Vaultly.Transactions.Infrastructure.Persistence;

namespace Vaultly.Transactions.Infrastructure.Transactions;

public sealed class TransactionReadService(TransactionsDbContext dbContext) : ITransactionReadService
{
    public async Task<TransactionPage> GetTransactionsAsync(
        OwnerId ownerId,
        AccountId? accountId,
        DateOnly? from,
        DateOnly? to,
        TransactionsCursor? after,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = OwnedBy(ownerId);
        if (accountId is { } account)
        {
            query = query.Where(x => x.AccountId == account);
        }

        if (from is { } first)
        {
            query = query.Where(x => x.BookedOn >= first);
        }

        if (to is { } last)
        {
            query = query.Where(x => x.BookedOn <= last);
        }

        if (after is { } cursor)
        {
            query = query.Where(x => x.BookedOn < cursor.BookedOn
                || (x.BookedOn == cursor.BookedOn && EF.Property<long>(x, TransactionConfiguration.Sequence) < cursor.Sequence));
        }

        var rows = await query
            .OrderByDescending(x => x.BookedOn)
            .ThenByDescending(x => EF.Property<long>(x, TransactionConfiguration.Sequence))
            .Select(x => new { Transaction = x, Sequence = EF.Property<long>(x, TransactionConfiguration.Sequence) })
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var page = rows.Take(limit).ToList();
        var next = rows.Count > limit ? new TransactionsCursor(page[^1].Transaction.BookedOn, page[^1].Sequence) : (TransactionsCursor?)null;
        return new TransactionPage([.. page.Select(row => ToDto(row.Transaction))], next);
    }

    public async Task<TransactionDto?> GetTransactionAsync(TransactionId transactionId, OwnerId ownerId, CancellationToken cancellationToken)
    {
        var transaction = await OwnedBy(ownerId).SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
        return transaction is null ? null : ToDto(transaction);
    }

    private IQueryable<Transaction> OwnedBy(OwnerId ownerId)
        => dbContext.Transactions.AsNoTracking().Where(x => x.OwnerId == ownerId && x.Status != TransactionStatus.Deleted);

    private static TransactionDto ToDto(Transaction transaction)
        => new(
            transaction.Id.Value,
            transaction.AccountId.Value,
            transaction.Amount,
            transaction.Currency.Code,
            transaction.BookedOn,
            transaction.Description.Value,
            transaction.Note?.Value,
            transaction.Status,
            transaction.PendingStep,
            transaction.DeclineReason,
            transaction.RecordedAt);
}
