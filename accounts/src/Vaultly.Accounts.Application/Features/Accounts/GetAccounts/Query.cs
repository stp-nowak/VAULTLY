using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.GetAccounts;

public sealed record GetAccountsQuery(Guid OwnerId, bool IncludeClosed) : IQuery<IReadOnlyList<AccountDto>>;
