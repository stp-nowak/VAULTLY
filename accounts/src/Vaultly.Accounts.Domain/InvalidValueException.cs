using Vaultly.Core;

namespace Vaultly.Accounts.Domain;

public sealed class InvalidValueException(string message) : DomainException(message);
