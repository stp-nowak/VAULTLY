using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

public sealed class GetTransactionsHandler(ITransactionReadService readService)
    : IQueryHandler<GetTransactionsQuery, TransactionPageDto>
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public async Task<TransactionPageDto> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var limit = request.Limit ?? DefaultLimit;
        if (limit is < 1 or > MaxLimit)
        {
            throw new InvalidPageRequestException($"Limit must be between 1 and {MaxLimit}.");
        }

        TransactionsCursor? after = null;
        if (request.Cursor is { } cursor)
        {
            after = TransactionsCursor.TryDecode(cursor, out var position)
                ? position
                : throw new InvalidPageRequestException("The cursor is not valid.");
        }

        var page = await readService.GetTransactionsAsync(
            new OwnerId(request.OwnerId),
            request.AccountId is { } accountId ? new AccountId(accountId) : null,
            request.From,
            request.To,
            after,
            limit,
            cancellationToken);

        return new TransactionPageDto(page.Items, page.Next?.Encode());
    }
}
