using Vaultly.Accounts.Domain.Accounts;

namespace Vaultly.Accounts.Api.Contracts;

public sealed record OpenAccountRequest(string Name, AccountType? Type, string Currency, decimal OpeningBalance);

public sealed record RenameAccountRequest(string Name);
