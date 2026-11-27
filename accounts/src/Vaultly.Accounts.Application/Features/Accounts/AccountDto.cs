using Vaultly.Accounts.Domain.Enums;

namespace Vaultly.Accounts.Application.Features.Accounts;

public sealed record AccountDto(
    Guid Id,
    string Name,
    AccountType Type,
    string Currency,
    decimal OpeningBalance,
    decimal Balance,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);
