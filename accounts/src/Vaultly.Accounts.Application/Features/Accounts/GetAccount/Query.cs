using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.GetAccount;

public sealed record GetAccountQuery(Guid OwnerId, Guid AccountId) : IQuery<AccountDto>;
