using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Domain.Postings;
using Vaultly.Accounts.PublicTypes.Commands;
using Vaultly.Core;
using Vaultly.Core.Amounts;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Postings.PostTransaction;

public sealed class PostTransactionHandler(IAccountRepository accounts, IPostingRepository postings, IUnitOfWork unitOfWork)
    : ICommandHandler<PostTransactionCommand, DeclineReason?>
{
    public async Task<DeclineReason?> Handle(PostTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = await accounts.GetAsync(new AccountId(request.AccountId), new OwnerId(request.OwnerId), cancellationToken);
        if (account is null)
        {
            return DeclineReason.AccountNotFound;
        }

        var posting = await postings.GetAsync(new TransactionId(request.TransactionId), cancellationToken);
        if (posting is not null && posting.HasApplied(request.StepId))
        {
            return null;
        }

        if (posting is not null && posting.AccountId != account.Id)
        {
            return DeclineReason.AccountNotFound;
        }

        if (request.Operation != PostingOperation.Delete && new Currency(request.Currency) != account.Currency)
        {
            return DeclineReason.CurrencyMismatch;
        }

        if (account.IsClosed)
        {
            return DeclineReason.AccountClosed;
        }

        if (posting is null)
        {
            posting = Posting.Start(new TransactionId(request.TransactionId), account.Id);
            postings.Add(posting);
        }

        var previousEffect = posting.Effect;
        if (request.Operation == PostingOperation.Delete)
        {
            posting.Delete(request.StepId);
        }
        else
        {
            posting.Book(new Money(request.Amount, account.Currency).Amount, request.BookedOn, request.StepId);
        }

        account.ApplyPosting(previousEffect, posting.Effect);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return null;
    }
}
