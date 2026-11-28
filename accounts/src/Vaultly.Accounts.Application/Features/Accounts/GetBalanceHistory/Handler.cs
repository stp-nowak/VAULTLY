using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;

public sealed class GetBalanceHistoryHandler(IBalanceHistoryReadService readService)
    : IQueryHandler<GetBalanceHistoryQuery, IReadOnlyList<DailyBalanceDto>>
{
    public async Task<IReadOnlyList<DailyBalanceDto>> Handle(GetBalanceHistoryQuery request, CancellationToken cancellationToken)
    {
        var movements = await readService.GetMovementsAsync(new AccountId(request.AccountId), new OwnerId(request.OwnerId), cancellationToken)
                        ?? throw new AccountNotFoundException(request.AccountId);

        var balance = movements.OpeningBalance;
        var history = new List<DailyBalanceDto>();
        foreach (var day in movements.DailyTotals)
        {
            balance += day.Amount;
            if ((request.From is null || day.Date >= request.From) && (request.To is null || day.Date <= request.To))
            {
                history.Add(new DailyBalanceDto(day.Date, balance));
            }
        }

        return history;
    }
}
