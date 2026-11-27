using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;

/// <summary>The owner's net worth per currency, from open accounts and without exchange rates.</summary>
public sealed record GetNetWorthQuery(Guid OwnerId) : IQuery<IReadOnlyList<CurrencyNetWorthDto>>;
